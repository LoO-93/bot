using AutoBot.Models.LnMarkets;
using AutoBot.Models.Trading;
using AutoBot.Models.Units;

namespace AutoBot.Services;

public interface ITradeManager
{
    UserModel? GetUser();

    AccountDetails? GetAccountDetails();

    void UpdateBtcPriceInUsd(Dollar price);

    Task HandlePriceUpdateAsync(LastPriceData data);

    Task<bool> CreateManagedPositionAsync(Satoshi amountInSats);
}
