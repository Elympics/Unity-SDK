using System;
using System.Collections.Generic;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using Elympics.Rooms.Models;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels.ToRooms
{
    [MessagePackObject]
    public record JoinWithJoinCodeDto(
        [property: Key(1)] string JoinCode,
        [property: Key(2)] uint? TeamIndex,
        [property: Key(3)] IReadOnlyDictionary<string, string>? CustomPlayerData) : LobbyOperation
    {
        [SerializationConstructor]
        public JoinWithJoinCodeDto(Guid operationId, string joinCode, uint? teamIndex, IReadOnlyDictionary<string, string>? customPlayerData) : this(joinCode, teamIndex, customPlayerData) =>
            OperationId = operationId;

        public virtual bool Equals(JoinWithJoinCodeDto? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return JoinCode == other.JoinCode
                && TeamIndex == other.TeamIndex
                && CustomPlayerData.IsTheSame(other.CustomPlayerData);
        }

        public override int GetHashCode() => HashCode.Combine(JoinCode, TeamIndex, CustomPlayerData?.Count);
    }
}
