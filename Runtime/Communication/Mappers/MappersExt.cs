using System.Collections;
using System.Collections.Generic;
using Elympics.Communication.PublicApi;
using Elympics.Rooms.Models;
using UnityEngine;

namespace Elympics.Communication.Mappers
{
    public static class Mappers
    {
        public static RoomCoinInfo ToRoomCoinInfo(this RoomCoin coin)
        {
            var chainInfo = new RoomChainInfo
            {
                ExternalId = coin.Chain.ExternalId,
                Type = (ChainTypeInfo)(int)coin.Chain.Type,
                Name = coin.Chain.Name
            };

            var currencyInfo = new RoomCurrencyInfo
            {
                Ticker = coin.Currency.Ticker,
                Address = coin.Currency.Address,
                Decimals = coin.Currency.Decimals,
                IconUrl = coin.Currency.IconUrl
            };

            var coinInfo = new RoomCoinInfo
            {
                CoinId = coin.CoinId,
                Chain = chainInfo,
                Currency = currencyInfo
            };
            return coinInfo;
        }
    }
}
