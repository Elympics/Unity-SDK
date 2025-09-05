using System;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.Models
{
    [MessagePackObject]
    public class RoomCurrencyDto
    {
        [Key(0)] public string Ticker { get; set; } = null!;
        [Key(1)] public string? Address { get; set; }
        [Key(2)] public int Decimals { get; set; }
        [Key(3)] public string IconUrl { get; set; } = null!;

        private bool Equals(RoomCurrencyDto other) => Ticker == other.Ticker && Address == other.Address && Decimals == other.Decimals && IconUrl == other.IconUrl;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is RoomCurrencyDto other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(Ticker, Address, Decimals, IconUrl);

        public static bool operator ==(RoomCurrencyDto? left, RoomCurrencyDto? right) => Equals(left, right);

        public static bool operator !=(RoomCurrencyDto? left, RoomCurrencyDto? right) => !Equals(left, right);
    }
}
