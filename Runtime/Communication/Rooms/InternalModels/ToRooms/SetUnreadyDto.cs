using System;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels.ToRooms
{
    [MessagePackObject]
    public record SetUnreadyDto([property: Key(1)] Guid RoomId) : LobbyOperation
    {
        [SerializationConstructor]
        public SetUnreadyDto(Guid operationId, Guid roomId) : this(roomId) => OperationId = operationId;

        public virtual bool Equals(SetUnreadyDto? other)
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
