namespace AutoBot.Models.Trading;

public readonly struct Margins
{
    public required long Initial { get; init; }

    public required long Maintenance { get; init; }
}
