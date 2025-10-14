namespace AutoBot.Models.Api;

public readonly struct UserBalanceResponse
{
    public required long BalanceInSats { get; init; }

    public required decimal SyntheticUsdBalance { get; init; }
}
