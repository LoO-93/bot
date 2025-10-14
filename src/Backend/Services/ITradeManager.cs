using AutoBot.Models.LnMarkets;

namespace AutoBot.Services;

public interface ITradeManager
{
    void UpdateBtcPriceInUsd(decimal price);

    Task HandlePriceUpdateAsync(LastPriceData data);

    Task<bool> CreateManagedPositionAsync(long amountInSats);
}
