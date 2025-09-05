using System;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.Models
{
    [MessagePackObject]
    public class RoomChainDto
    {
        [Key(0)] public int ExternalId { get; set; }
        [Key(1)] public ChainTypeDto Type { get; set; }
        [Key(2)] public string Name { get; set; }

        private bool Equals(RoomChainDto other) => ExternalId == other.ExternalId && Type == other.Type && Name == other.Name;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is RoomChainDto other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(ExternalId, (int)Type, Name);

        public static bool operator ==(RoomChainDto? left, RoomChainDto? right) => Equals(left, right);

        public static bool operator !=(RoomChainDto? left, RoomChainDto? right) => !Equals(left, right);
    }
}
