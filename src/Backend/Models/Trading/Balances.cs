namespace AutoBot.Models.Trading;

public readonly struct Balances
{
    public required decimal sUSD { get; init; }

    public required long Cross { get; init; }

    public required long Isolated { get; init; }

    public required long Available { get; init; }
}
