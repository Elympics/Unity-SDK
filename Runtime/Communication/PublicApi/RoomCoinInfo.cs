#nullable enable
using System;

namespace Elympics.Communication.PublicApi
{
    public struct RoomCoinInfo
    {
        public Guid CoinId { get; init; }
        public RoomChainInfo Chain { get; init; }
        public RoomCurrencyInfo Currency { get; init; }
    }

    public struct RoomChainInfo
    {
        public int ExternalId { get; init; }
        public ChainTypeInfo Type { get; init; }
        public string Name { get; init; }
    }

    public struct RoomCurrencyInfo
    {
        public string Ticker { get; init; }
        public string? Address { get; init; }
        public int Decimals { get; init; }
        public string IconUrl { get; init; }
    }

    public enum ChainTypeInfo
    {
        Ton = 0,
        Evm = 1,
    }
}
