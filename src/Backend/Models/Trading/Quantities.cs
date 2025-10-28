using AutoBot.Models.Units;

namespace AutoBot.Models.Trading;

public readonly struct Quantities
{
    public required Dollar Total { get; init; }

    public required Dollar Cross { get; init; }

    public required Dollar Open { get; init; }

    public required Dollar Running { get; init; }
}
