using AutoBot.Models.Units;

namespace AutoBot.Models.Trading;

public readonly struct Margins
{
    public required Satoshi Open { get; init; }

    public required Satoshi Running { get; init; }

    public required Satoshi Maintenance { get; init; }

    public required Satoshi Total { get; init; }
}
