using AutoBot.Models.Units;

namespace AutoBot.Models.Trading;

public readonly struct AccountDetails
{
    public required Satoshi TotalNetValue { get; init; }

    public required Balances Balances { get; init; }

    public required Quantities TotalQuantity { get; init; }

    public required Margins Margins { get; init; }

    public required Satoshi ProfitLoss { get; init; }

    public required Dollar CurrentPrice { get; init; }
}
