namespace AutoBot.Models.Trading;

public readonly struct Quantities
{
    public required decimal Total { get; init; }

    public required decimal Cross { get; init; }

    public required decimal Isolated { get; init; }
}
