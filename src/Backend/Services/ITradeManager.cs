using AutoBot.Models.LnMarkets;

namespace AutoBot.Services;

public interface ITradeManager
{
    UserModel? GetUser();

    void UpdateBtcPriceInUsd(decimal price);

    Task HandlePriceUpdateAsync(LastPriceData data);

    Task<bool> CreateManagedPositionAsync(long amountInSats);
}
