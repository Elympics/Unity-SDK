using System;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels.ToRooms
{
    [MessagePackObject]
    public record StartMatchmakingDto([property: Key(1)] Guid RoomId) : LobbyOperation
    {
        [SerializationConstructor]
        public StartMatchmakingDto(Guid operationId, Guid roomId) : this(roomId) => OperationId = operationId;

        public virtual bool Equals(StartMatchmakingDto? other)
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
