#nullable enable

using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.Rooms.Models;

namespace Elympics.Communication.Mappers
{
    internal static class Mappers
    {
        internal static async UniTask<CoinInfo> ToCoinInfo(this RoomCoin coin, ElympicsLoggerContext logger)
        {
            var chainInfo = new ChainInfo
            {
                ExternalId = coin.Chain.ExternalId,
                Type = coin.Chain.Type.ToString(),
                Name = coin.Chain.Name
            };

            var currencyInfo = new CurrencyInfo
            {
                Ticker = coin.Currency.Ticker,
                Address = coin.Currency.Address,
                Decimals = coin.Currency.Decimals,
                Icon = await CoinIcons.GetIconOrNull(coin.CoinId, coin.Currency.IconUrl, logger)
            };

            var coinInfo = new CoinInfo
            {
                Id = coin.CoinId,
                Chain = chainInfo,
                Currency = currencyInfo
            };
            return coinInfo;
        }
    }
}
