namespace AutoBot.Models.Trading;

public readonly struct Margins
{
    public required long Open { get; init; }

    public required long Running { get; init; }

    public required long Maintenance { get; init; }

    public required long Total { get; init; }
}
