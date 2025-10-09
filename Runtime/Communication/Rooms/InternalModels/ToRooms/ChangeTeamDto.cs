using System;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels.ToRooms
{
    [MessagePackObject]
    public record ChangeTeamDto(
        [property: Key(1)] Guid RoomId,
        [property: Key(2)] uint? TeamIndex) : LobbyOperation
    {
        [SerializationConstructor]
        public ChangeTeamDto(Guid operationId, Guid roomId, uint? teamIndex) : this(roomId, teamIndex) =>
            OperationId = operationId;

        public virtual bool Equals(ChangeTeamDto? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return RoomId == other.RoomId
                && TeamIndex == other.TeamIndex;
        }

        public override int GetHashCode() => HashCode.Combine(RoomId, TeamIndex);
    }
}
