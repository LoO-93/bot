using AutoBot.Models.Trading;

namespace AutoBot.Models.Api;

public readonly struct AccountDetailsResponse
{
    public required long TotalNetValue { get; init; }

    public required Balances Balances { get; init; }

    public required Quantities TotalQuantity { get; init; }

    public required Margins Margins { get; init; }

    public required long ProfitLoss { get; init; }

    public required decimal CurrentPrice { get; init; }
}
