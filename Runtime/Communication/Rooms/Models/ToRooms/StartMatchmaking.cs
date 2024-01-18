using System;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record StartMatchmaking([property: Key(1)] Guid RoomId) : LobbyOperation
    {
        [SerializationConstructor]
        public StartMatchmaking(Guid operationId, Guid roomId) : this(roomId) => OperationId = operationId;

        public virtual bool Equals(StartMatchmaking? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return RoomId == other.RoomId;
        }

        public override int GetHashCode() => HashCode.Combine(RoomId);
    }
}
