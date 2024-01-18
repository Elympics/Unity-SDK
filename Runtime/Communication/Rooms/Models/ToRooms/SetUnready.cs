using System;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record SetUnready([property: Key(1)] Guid RoomId) : LobbyOperation
    {
        [SerializationConstructor]
        public SetUnready(Guid operationId, Guid roomId) : this(roomId) => OperationId = operationId;

        public virtual bool Equals(SetUnready? other)
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
