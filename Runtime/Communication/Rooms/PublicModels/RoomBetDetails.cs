using System;
using Elympics.Util;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record RoomBetDetails([property: Key(0)] string BetValueRaw, [property: Key(1)] RoomCoin Coin, [property: Key(2)] RollingBet? RollingBet)
    {
        [IgnoreMember]
        public decimal BetValue => RawCoinConverter.FromRaw(BetValueRaw, Coin.Currency.Decimals);

        public virtual bool Equals(RoomBetDetails? other) => other != null && BetValueRaw == other.BetValueRaw && Coin.Equals(other.Coin);

        public override int GetHashCode() => HashCode.Combine(BetValue, Coin);

        public override string ToString() => $"${nameof(BetValue)}:{BetValue}{Environment.NewLine}"
                                             + $"{nameof(Coin)}: Id: {Coin.CoinId} | Ticker: {Coin.Currency.Ticker} | ChainType: {Coin.Chain.Type}";
    }
}
