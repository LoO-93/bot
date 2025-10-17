using AutoBot.Models.LnMarkets;
using AutoBot.Models.Trading;

namespace AutoBot.Services;

public interface ITradeManager
{
    UserModel? GetUser();

    AccountDetails? GetAccountDetails();

    void UpdateBtcPriceInUsd(decimal price);

    Task HandlePriceUpdateAsync(LastPriceData data);

    Task<bool> CreateManagedPositionAsync(long amountInSats);
}
