namespace AutoBot.Models.Trading;

public readonly struct AccountDetails
{
    public required long TotalNetValue { get; init; }

    public required Balances Balances { get; init; }

    public required Quantities TotalQuantity { get; init; }

    public required Margins Margins { get; init; }

    public required long ProfitLoss { get; init; }

    public required decimal CurrentPrice { get; init; }
}
