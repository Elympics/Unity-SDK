using System;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public class RoomCoin
    {
        [Key(0)] public Guid CoinId { get; set; }
        [Key(1)] public RoomChain Chain { get; set; }
        [Key(2)] public RoomCurrency Currency { get; set; }

        private bool Equals(RoomCoin other) => CoinId.Equals(other.CoinId) && Chain.Equals(other.Chain) && Currency.Equals(other.Currency);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is RoomCoin other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(CoinId, Chain, Currency);

        public static bool operator ==(RoomCoin? left, RoomCoin? right) => Equals(left, right);

        public static bool operator !=(RoomCoin? left, RoomCoin? right) => !Equals(left, right);
    }
}
