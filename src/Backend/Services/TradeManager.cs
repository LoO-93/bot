using AutoBot.Models;
using AutoBot.Models.LnMarkets;
using AutoBot.Models.Trading;
using AutoBot.Models.Units;
using Microsoft.Extensions.Options;

namespace AutoBot.Services;

public class TradeManager : ITradeManager
{
    private readonly IMarketplaceClient _client;
    private readonly IOptionsMonitor<LnMarketsOptions> _options;
    private readonly ILogger<TradeManager> _logger;

    private readonly object _priceLock = new();
    private readonly object _userLock = new();
    private readonly object _tradesLock = new();

    private DateTime _lastConfigChange = DateTime.MinValue;
    private decimal _latestPrice = 0;
    private UserModel? _latestUser = null;
    private IReadOnlyList<FuturesTradeModel>? _latestOpenTrades = null;
    private IReadOnlyList<FuturesTradeModel>? _latestRunningTrades = null;

    public TradeManager(IMarketplaceClient client, IOptionsMonitor<LnMarketsOptions> options, ILogger<TradeManager> logger)
    {
        _client = client;
        _options = options;
        _logger = logger;

        // Log configuration changes with 500ms debouncing (.OnChange triggers multiple times for the same change...)
        options.OnChange(newOptions =>
        {
            var now = DateTime.UtcNow;
            if ((now - _lastConfigChange).TotalMilliseconds > 500)
            {
                _logger.LogWarning(
                    "LnMarketsOptions configuration updated:\n\tPause={},\n\tQuantity={},\n\tLeverage={},\n\tTakeProfit={},\n\tMaxTakeprofitPrice={},\n\tMaxRunningTrades={},\n\tFactor={},\n\tAddMarginInUsd={}",
                    newOptions.Pause,
                    newOptions.Quantity,
                    newOptions.Leverage,
                    newOptions.Takeprofit,
                    newOptions.MaxTakeprofitPrice,
                    newOptions.MaxRunningTrades,
                    newOptions.Factor,
                    newOptions.AddMarginInUsd);
                _lastConfigChange = now;
            }
        });
    }

    public UserModel? GetUser()
    {
        lock (_userLock)
        {
            return _latestUser?.Clone();
        }
    }

    public AccountDetails? GetAccountDetails()
    {
        UserModel? user;
        decimal currentPrice;
        IReadOnlyList<FuturesTradeModel>? openTrades;
        IReadOnlyList<FuturesTradeModel>? runningTrades;

        lock (_userLock)
        {
            user = _latestUser?.Clone();
        }

        lock (_priceLock)
        {
            currentPrice = _latestPrice;
        }

        lock (_tradesLock)
        {
            openTrades = _latestOpenTrades;
            runningTrades = _latestRunningTrades;
        }

        if (user == null)
        {
            return null;
        }

        openTrades ??= [];
        runningTrades ??= [];

        var openMarginInSats = decimal.ToInt64(openTrades.Sum(t => t.margin));
        var openMaintenanceMarginInSats = decimal.ToInt64(openTrades.Sum(t => t.maintenance_margin));

        var runningMarginInSats = decimal.ToInt64(runningTrades.Sum(t => t.margin));
        var runningMaintenanceMarginInSats = decimal.ToInt64(runningTrades.Sum(t => t.maintenance_margin));

        var totalMarginInSats = runningMarginInSats + openMarginInSats;
        var totalMaintenanceMarginInSats = runningMaintenanceMarginInSats + openMaintenanceMarginInSats;
        var isolatedMarginInSats = totalMarginInSats + totalMaintenanceMarginInSats;

        var openQuantity = openTrades.Sum(t => t.quantity);
        var runningQuantity = runningTrades.Sum(t => t.quantity);
        var totalQuantity = openQuantity + runningQuantity;

        var totalPLInSats = decimal.ToInt64(runningTrades.Sum(t => t.pl));

        var availableBalance = Math.Max(0, decimal.ToInt64(user.balance) - isolatedMarginInSats);

        var totalNetValue = decimal.ToInt64(user.balance) + totalPLInSats;

        return new AccountDetails
        {
            TotalNetValue = totalNetValue,
            Balances = new Balances
            {
                sUSD = user.synthetic_usd_balance,
                Cross = 0, // LN Markets uses isolated margin model,
                Isolated = isolatedMarginInSats,
                Available = availableBalance,
            },
            TotalQuantity = new Quantities
            {
                Total = totalQuantity,
                Cross = 0, // LN Markets uses isolated margin model
                Open = openQuantity,
                Running = runningQuantity,
            },
            Margins = new Margins
            {
                Open = openMarginInSats,
                OpenMaintenance = openMaintenanceMarginInSats,
                Running = runningMarginInSats,
                RunningMaintenance = runningMaintenanceMarginInSats,
                Total = totalMarginInSats,
                TotalMaintenance = totalMaintenanceMarginInSats,
            },
            ProfitLoss = totalPLInSats,
            CurrentPrice = currentPrice,
        };
    }

    public void UpdateBtcPriceInUsd(decimal price)
    {
        lock (_priceLock)
        {
            _latestPrice = price;
        }
    }

    public async Task HandlePriceUpdateAsync(LastPriceData data)
    {
        _logger.LogInformation("Handling price update: {Price}$", data.LastPrice);

        var user = await _client.GetUser(_options.CurrentValue.Key, _options.CurrentValue.Passphrase, _options.CurrentValue.Secret);
        if (user == null)
        {
            return;
        }

        var openTrades = await _client.GetOpenTrades(_options.CurrentValue.Key, _options.CurrentValue.Passphrase, _options.CurrentValue.Secret);
        var runningTrades = await _client.GetRunningTrades(_options.CurrentValue.Key, _options.CurrentValue.Passphrase, _options.CurrentValue.Secret);

        lock (_userLock)
        {
            _latestUser = user;
        }

        lock (_tradesLock)
        {
            _latestOpenTrades = openTrades;
            _latestRunningTrades = runningTrades;
        }

        if (_options.CurrentValue.Pause)
        {
            return;
        }

        if (user.balance == 0)
        {
            return;
        }

        await ProcessMarginManagement(_client, _options.CurrentValue, data, user, _logger);
        await ProcessTradeExecution(_client, _options.CurrentValue, data, user, _logger);
    }

    public async Task<bool> CreateManagedPositionAsync(long amountInSats)
    {
        try
        {
            var options = _options.CurrentValue;

            _logger.LogInformation("Creating managed position with {Amount} sats", amountInSats);

            // Get current user balance
            var user = await _client.GetUser(options.Key, options.Passphrase, options.Secret);
            if (user == null)
            {
                _logger.LogError("Failed to retrieve user information for managed position");
                return false;
            }

            // if (user.balance < amountInSats)
            // {
            //     _logger.LogWarning("Insufficient balance for managed position: required {Amount} sats | available: {Available} sats", amountInSats, user.balance);
            //     return false;
            // }

            // Calculate amounts: 50% for swap, 50% for trade
            var halfAmountInSats = amountInSats / 2;

            // Get current BTC price from stored latest price
            decimal currentPrice;
            lock (_priceLock)
            {
                currentPrice = _latestPrice;
            }

            if (currentPrice <= 0)
            {
                _logger.LogError("No current BTC price available for managed position");
                return false;
            }

            // Convert swap amount to USD
            var swapAmountInUsd = (int)Math.Floor((halfAmountInSats * currentPrice) / Constants.SatoshisPerBitcoin);
            if (swapAmountInUsd > 0)
            {
                // Step 1: Swap half to synthetic USD
                if (!await _client.SwapBtcInUsd(options.Key, options.Passphrase, options.Secret, swapAmountInUsd))
                {
                    _logger.LogError("Failed to swap {Amount}$ from BTC for managed position", swapAmountInUsd);
                    return false;
                }

                _logger.LogInformation("Successfully swapped {Amount}$ from BTC to synthetic USD", swapAmountInUsd);
            }

            // Step 2: Create trade with remaining amount
            var exitPrice = currentPrice + options.Takeprofit;

            // Calculate trade quantity based on remaining sats
            var tradeQuantity = (int)Math.Floor((halfAmountInSats * currentPrice) / (Constants.SatoshisPerBitcoin * options.Leverage));
            if (tradeQuantity <= 0)
            {
                _logger.LogWarning("Calculated trade quantity is 0 or negative for managed position");
                return false;
            }

            if (!await _client.CreateLimitBuyOrder(options.Key, options.Passphrase, options.Secret, currentPrice, exitPrice, options.Leverage, tradeQuantity))
            {
                _logger.LogError("Failed to create trade for managed position: [price: {Price}, exitPrice: {ExitPrice}, leverage: {Leverage}, quantity: {Quantity}]", currentPrice, exitPrice, options.Leverage, tradeQuantity);
                return false;
            }

            _logger.LogInformation("Successfully created managed position: swapped {SwapAmount}$ to sUSD and created trade with {TradeQuantity} quantity at {Price}$", swapAmountInUsd, tradeQuantity, currentPrice);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating managed position");
            return false;
        }
    }

    private static async Task ProcessMarginManagement(IMarketplaceClient client, LnMarketsOptions options, LastPriceData data, UserModel user, ILogger? logger = null)
    {
        try
        {
            var runningTrades = await client.GetRunningTrades(options.Key, options.Passphrase, options.Secret);
            if (runningTrades.Count == 0)
            {
                return;
            }

            Satoshi oneUsdInSats = decimal.ToInt64(Math.Round(Constants.SatoshisPerBitcoin.Value / data.LastPrice.Value));
            var marginCallTrades = runningTrades
                .Where(x => x.leverage > 1) // Skip trades with 1x leverage
                .Where(x => x.margin > 0) // Skip trades with invalid margin to prevent division by zero
                .Where(x => (decimal)x.pl.Value / x.margin.Value * 100 <= options.MaxLossInPercent) // Include trades where loss is worse than threshold
                .ToList();

            if (marginCallTrades.Count == 0)
            {
                return;
            }

            Satoshi oneMarginCallInSats = decimal.ToInt64(Math.Round(oneUsdInSats.Value * options.AddMarginInUsd, MidpointRounding.AwayFromZero));
            if (oneMarginCallInSats.Value * marginCallTrades.Count > user.balance)
            {
                logger?.LogWarning("Total amount for margin calls exceeds the available balance. Defaulting to FIFO margin call execution.");
            }

            Satoshi totalAddedMarginInSats = 0;
            Dollar totalAddedMarginInUsd = 0;
            foreach (var trade in marginCallTrades)
            {
                Satoshi maxMarginInSats = decimal.ToInt64(Math.Round(Constants.SatoshisPerBitcoin.Value / trade.price.Value * trade.quantity.Value, MidpointRounding.AwayFromZero));
                if (oneMarginCallInSats + trade.margin > maxMarginInSats)
                {
                    logger?.LogWarning("Margin call of {MarginCall} sats would exceed maximum margin of {MaxMargin} sats for trade {Id}", oneMarginCallInSats, maxMarginInSats, trade.id);
                    continue;
                }

                if (oneMarginCallInSats > user.balance)
                {
                    logger?.LogWarning("Insufficient available balance to execute margin call for trade {Id}: required {Margin} sats | available {Balance} sats", trade.id, oneMarginCallInSats, user.balance);
                    continue;
                }

                if (!await client.AddMarginInSats(options.Key, options.Passphrase, options.Secret, trade.id, oneMarginCallInSats))
                {
                    logger?.LogError("Failed to add margin {Margin} sats to running trade {Id}", oneMarginCallInSats, trade.id);
                    continue;
                }

                user.balance -= oneMarginCallInSats;
                totalAddedMarginInSats += oneMarginCallInSats;
                totalAddedMarginInUsd += options.AddMarginInUsd;
                logger?.LogInformation("Successfully added margin {Margin} sats to running trade {Id}", oneMarginCallInSats, trade.id);
            }

            if (totalAddedMarginInUsd <= 0)
            {
                return;
            }

            if (user.synthetic_usd_balance < totalAddedMarginInUsd)
            {
                logger?.LogDebug("No enough synthetic usd balance available for swap: required {Amount}$ | available {Available}$", totalAddedMarginInUsd, user.synthetic_usd_balance);
                return;
            }

            if (!await client.SwapUsdInBtc(options.Key, options.Passphrase, options.Secret, decimal.ToInt32(Math.Round(totalAddedMarginInUsd.Value, MidpointRounding.AwayFromZero))))
            {
                logger?.LogError("Failed to swap {Amount}$ to btc", totalAddedMarginInUsd);
                return;
            }

            user.balance += totalAddedMarginInSats;
            user.synthetic_usd_balance -= totalAddedMarginInUsd;
            logger?.LogInformation("Successfully swapped {Amount}$ to btc", totalAddedMarginInUsd);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during margin management");
        }
    }

    private static async Task ProcessTradeExecution(IMarketplaceClient client, LnMarketsOptions options, LastPriceData data, UserModel user, ILogger? logger = null)
    {
        try
        {
            if (data.LastPrice <= 0)
            {
                logger?.LogWarning("Invalid last price: {Price}", data.LastPrice);
                return;
            }

            var runningTrades = await client.GetRunningTrades(options.Key, options.Passphrase, options.Secret);
            if (runningTrades.Count >= options.MaxRunningTrades)
            {
                logger?.LogDebug("Maximum number of running trades has been reached ({MaxRunningTrades})", options.MaxRunningTrades);
                return;
            }

            Dollar quantizedPriceInUsd = Math.Floor(data.LastPrice.Value / options.Factor) * options.Factor;
            var runningTrade = runningTrades.FirstOrDefault(x => x.price == quantizedPriceInUsd);
            if (runningTrade != null)
            {
                logger?.LogDebug("A running trade with the same price already exists ({Price}$)", quantizedPriceInUsd);
                return;
            }

            // Only count running trade margins - open trades will be canceled and their margin freed
            Satoshi isolatedMarginInSats = runningTrades.Select(x => x.margin.Value + x.maintenance_margin.Value).Sum();
            Satoshi availableMarginInSats = user.balance - isolatedMarginInSats;

            Satoshi oneUsdInSats = decimal.ToInt64(Math.Round(Constants.SatoshisPerBitcoin.Value / data.LastPrice.Value));
            if (availableMarginInSats <= oneUsdInSats)
            {
                logger?.LogDebug("No available margin");
                return;
            }

            var openTrades = await client.GetOpenTrades(options.Key, options.Passphrase, options.Secret);
            var openTrade = openTrades.FirstOrDefault(x => x.price == quantizedPriceInUsd);
            if (openTrade != null)
            {
                logger?.LogDebug("An open trade with the same price already exists ({Price}$)", quantizedPriceInUsd);
                return;
            }

            Dollar exitPriceInUsd;
            if (!options.TargetNetPLInSats.HasValue)
            {
                exitPriceInUsd = quantizedPriceInUsd + options.Takeprofit;
            }
            else
            {
                var feeRate = GetFeeRateFromTier(user.fee_tier);
                logger?.LogDebug("User fee tier {FeeTier} mapped to fee rate {FeeRate:P}", user.fee_tier, feeRate);

                Satoshi targetNetPLInSats = options.TargetNetPLInSats.Value;
                decimal adjustedExitPriceInUsd = TradeFactory.CalculateExitPriceForTargetNetPL(options.Quantity, quantizedPriceInUsd.Value, options.Leverage, feeRate, targetNetPLInSats.Value, TradeSide.Buy);
                Dollar roundedExitPriceInUsd = Math.Ceiling(adjustedExitPriceInUsd * 2) / 2; // Round up to nearest 0.5 for LN Markets compatibility
                logger?.LogDebug("Adjusted exit price to {AdjustedExitPrice}$ for a net P&L of {TargetProfit} sats", roundedExitPriceInUsd, targetNetPLInSats);

                exitPriceInUsd = roundedExitPriceInUsd;
            }

            if (exitPriceInUsd >= options.MaxTakeprofitPrice)
            {
                logger?.LogDebug("Exit price {ExitPrice}$ exceeds maximum take profit price {MaximumPrice}$", exitPriceInUsd, options.MaxTakeprofitPrice);
                return;
            }

            Satoshi requiredMarginInSats = decimal.ToInt64(Math.Ceiling(Constants.SatoshisPerBitcoin.Value / quantizedPriceInUsd.Value * options.Quantity / options.Leverage));
            if (requiredMarginInSats > availableMarginInSats)
            {
                logger?.LogWarning("Insufficient margin: required {RequiredMargin} sats | available {AvailableMargin} sats", requiredMarginInSats, availableMarginInSats);
                return;
            }

            foreach (var oldTrade in openTrades)
            {
                if (!await client.Cancel(options.Key, options.Passphrase, options.Secret, oldTrade.id))
                {
                    logger?.LogWarning("Failed to cancel trade {TradeId}", oldTrade.id);
                }
            }

            if (!await client.CreateLimitBuyOrder(options.Key, options.Passphrase, options.Secret, quantizedPriceInUsd.Value, exitPriceInUsd.Value, options.Leverage, options.Quantity))
            {
                logger?.LogError("Failed to create limit buy order:\n\t[price: {Price}, takeprofit: {TakeProfit}, leverage: {Leverage}, quantity: {Quantity}]", quantizedPriceInUsd, exitPriceInUsd, options.Leverage, options.Quantity);
                return;
            }

            logger?.LogInformation("Successfully created limit buy order:\n\t[price: {Price}, takeprofit: {TakeProfit}, leverage: {Leverage}, quantity: {Quantity}]", quantizedPriceInUsd, exitPriceInUsd, options.Leverage, options.Quantity);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during trade execution");
        }
    }

    private static decimal GetFeeRateFromTier(decimal feeTier)
    {
        // LN Markets fee tiers (based on 30-day cumulative volume):
        // API returns 0-indexed tiers:
        // 0 = Tier 1: 0 volume → 0.1% fee
        // 1 = Tier 2: > $250k → 0.08% fee
        // 2 = Tier 3: > $1,000k → 0.07% fee
        // 3 = Tier 4: > $5,000k → 0.06% fee
        return feeTier switch
        {
            0 => 0.001m,   // Tier 1: 0.1%
            1 => 0.0008m,  // Tier 2: 0.08%
            2 => 0.0007m,  // Tier 3: 0.07%
            3 => 0.0006m,  // Tier 4: 0.06%
            _ => 0.001m,   // Default to highest fee rate for safety
        };
    }
}
