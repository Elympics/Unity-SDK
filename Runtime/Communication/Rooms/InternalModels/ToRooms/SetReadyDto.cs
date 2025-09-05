using System;
using System.Linq;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels.ToRooms
{
    [MessagePackObject]
    public record SetReadyDto(
        [property: Key(1)] Guid RoomId,
        [property: Key(2)] byte[] GameEngineData,
        [property: Key(3)] float[] MatchmakerData,
        [property: Key(4)] DateTime LastRoomUpdate) : LobbyOperation
    {
        [SerializationConstructor]
        public SetReadyDto(Guid operationId, Guid roomId, byte[] gameEngineData, float[] matchmakerData, DateTime lastRoomUpdate) : this(roomId, gameEngineData, matchmakerData, lastRoomUpdate) =>
            OperationId = operationId;

        public virtual bool Equals(SetReadyDto? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return RoomId == other.RoomId
                && LastRoomUpdate == other.LastRoomUpdate
                && GameEngineData.SequenceEqual(other.GameEngineData)
                && MatchmakerData.SequenceEqual(other.MatchmakerData);
        }

        public override int GetHashCode() => HashCode.Combine(RoomId, GameEngineData.Length, MatchmakerData.Length, LastRoomUpdate);
    }
}
