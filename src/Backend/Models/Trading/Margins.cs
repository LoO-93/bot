namespace AutoBot.Models.Trading;

public readonly struct Margins
{
    public required long Open { get; init; }

    public required long OpenMaintenance { get; init; }

    public required long Running { get; init; }

    public required long RunningMaintenance { get; init; }

    public required long Total { get; init; }

    public required long TotalMaintenance { get; init; }
}
