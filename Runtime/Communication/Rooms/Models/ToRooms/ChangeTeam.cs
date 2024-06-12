using System;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record ChangeTeam(
        [property: Key(1)] Guid RoomId,
        [property: Key(2)] uint? TeamIndex) : LobbyOperation
    {
        [SerializationConstructor]
        public ChangeTeam(Guid operationId, Guid roomId, uint? teamIndex) : this(roomId, teamIndex) =>
            OperationId = operationId;

        public virtual bool Equals(ChangeTeam? other)
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
