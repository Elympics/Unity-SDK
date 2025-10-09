using System;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public class RoomCurrency
    {
        [Key(0)] public string Ticker { get; set; } = null!;
        [Key(1)] public string? Address { get; set; }
        [Key(2)] public int Decimals { get; set; }
        [Key(3)] public string IconUrl { get; set; } = null!;

        private bool Equals(RoomCurrency other) => Ticker == other.Ticker && Address == other.Address && Decimals == other.Decimals && IconUrl == other.IconUrl;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is RoomCurrency other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(Ticker, Address, Decimals, IconUrl);

        public static bool operator ==(RoomCurrency? left, RoomCurrency? right) => Equals(left, right);

        public static bool operator !=(RoomCurrency? left, RoomCurrency? right) => !Equals(left, right);
    }
}
