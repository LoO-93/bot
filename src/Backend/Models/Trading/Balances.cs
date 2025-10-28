using AutoBot.Models.Units;

namespace AutoBot.Models.Trading;

public readonly struct Balances
{
    public required Dollar sUSD { get; init; }

    public required Satoshi Cross { get; init; }

    public required Satoshi Isolated { get; init; }

    public required Satoshi Available { get; init; }
}
