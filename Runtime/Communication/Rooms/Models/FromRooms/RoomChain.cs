using System;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public class RoomChain
    {
        [Key(0)] public int ExternalId { get; set; }
        [Key(1)] public ChainType Type { get; set; }
        [Key(2)] public string Name { get; set; }

        private bool Equals(RoomChain other) => ExternalId == other.ExternalId && Type == other.Type && Name == other.Name;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is RoomChain other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(ExternalId, (int)Type, Name);

        public static bool operator ==(RoomChain? left, RoomChain? right) => Equals(left, right);

        public static bool operator !=(RoomChain? left, RoomChain? right) => !Equals(left, right);
    }
}
