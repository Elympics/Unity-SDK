using System;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record JoinWithJoinCode(
        [property: Key(1)] string JoinCode,
        [property: Key(2)] uint? TeamIndex) : LobbyOperation
    {
        [SerializationConstructor]
        public JoinWithJoinCode(Guid operationId, string joinCode, uint? teamIndex) : this(joinCode, teamIndex) =>
            OperationId = operationId;

        public virtual bool Equals(JoinWithJoinCode? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return JoinCode == other.JoinCode
                && TeamIndex == other.TeamIndex;
        }

        public override int GetHashCode() => HashCode.Combine(JoinCode, TeamIndex);
    }
}
