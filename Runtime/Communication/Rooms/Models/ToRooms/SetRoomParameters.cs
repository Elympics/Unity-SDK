using System;
using System.Collections.Generic;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record SetRoomParameters(
        [property: Key(1)] Guid RoomId,
        [property: Key(2)] string? RoomName,
        [property: Key(3)] bool? IsPrivate,
        [property: Key(4)] IReadOnlyDictionary<string, string>? CustomRoomData,
        [property: Key(5)] IReadOnlyDictionary<string, string>? CustomMatchmakingData) : LobbyOperation
    {
        [SerializationConstructor]
        public SetRoomParameters(
            Guid operationId,
            Guid roomId,
            string? roomName,
            bool? isPrivate,
            IReadOnlyDictionary<string, string>? customRoomData,
            IReadOnlyDictionary<string, string>? customMatchmakingData) : this(roomId, roomName, isPrivate, customRoomData, customMatchmakingData) =>
            OperationId = operationId;

        public virtual bool Equals(SetRoomParameters? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return RoomId == other.RoomId
                && RoomName == other.RoomName
                && IsPrivate == other.IsPrivate
                && CustomRoomData.IsTheSame(other.CustomRoomData)
                && CustomMatchmakingData.IsTheSame(other.CustomMatchmakingData);
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), RoomId, RoomName ?? "", IsPrivate, CustomRoomData?.Count ?? 0, CustomMatchmakingData?.Count ?? 0);
    }
}
