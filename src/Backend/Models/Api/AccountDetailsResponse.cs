using AutoBot.Models.Trading;
using AutoBot.Models.Units;

namespace AutoBot.Models.Api;

public readonly struct AccountDetailsResponse
{
    public required Satoshi TotalNetValue { get; init; }

    public required Balances Balances { get; init; }

    public required Quantities TotalQuantity { get; init; }

    public required Margins Margins { get; init; }

    public required Satoshi ProfitLoss { get; init; }

    public required Dollar CurrentPrice { get; init; }
}
