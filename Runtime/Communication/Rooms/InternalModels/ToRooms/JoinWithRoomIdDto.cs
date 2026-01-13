using System;
using System.Collections.Generic;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using Elympics.Rooms.Models;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels.ToRooms
{
    [MessagePackObject]
    public record JoinWithRoomIdDto(
        [property: Key(1)] Guid RoomId,
        [property: Key(2)] uint? TeamIndex,
        [property: Key(3)] IReadOnlyDictionary<string, string>? CustomPlayerData) : LobbyOperation
    {
        [SerializationConstructor]
        public JoinWithRoomIdDto(Guid operationId, Guid roomId, uint? teamIndex, IReadOnlyDictionary<string, string>? customPlayerData) : this(roomId, teamIndex, customPlayerData) =>
            OperationId = operationId;

        public virtual bool Equals(JoinWithRoomIdDto? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return RoomId == other.RoomId
                && TeamIndex == other.TeamIndex
                && CustomPlayerData.IsTheSame(other.CustomPlayerData);
        }

        public override int GetHashCode() => HashCode.Combine(RoomId, TeamIndex, CustomPlayerData?.Count);
    }
}
