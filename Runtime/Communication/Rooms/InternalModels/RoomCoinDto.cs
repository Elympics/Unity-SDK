using System;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.Models
{
    [MessagePackObject]
    public class RoomCoinDto
    {
        [Key(0)] public Guid CoinId { get; set; }
        [Key(1)] public RoomChainDto Chain { get; set; }
        [Key(2)] public RoomCurrencyDto Currency { get; set; }

        private bool Equals(RoomCoinDto other) => CoinId.Equals(other.CoinId) && Chain.Equals(other.Chain) && Currency.Equals(other.Currency);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is RoomCoinDto other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(CoinId, Chain, Currency);

        public static bool operator ==(RoomCoinDto? left, RoomCoinDto? right) => Equals(left, right);

        public static bool operator !=(RoomCoinDto? left, RoomCoinDto? right) => !Equals(left, right);
    }
}
