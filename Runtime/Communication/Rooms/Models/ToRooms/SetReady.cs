using System;
using System.Linq;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record SetReady(
        [property: Key(1)] Guid RoomId,
        [property: Key(2)] byte[] GameEngineData,
        [property: Key(3)] float[] MatchmakerData) : LobbyOperation
    {
        [SerializationConstructor]
        public SetReady(Guid operationId, Guid roomId, byte[] gameEngineData, float[] matchmakerData) : this(roomId, gameEngineData, matchmakerData) =>
            OperationId = operationId;

        public virtual bool Equals(SetReady? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return RoomId == other.RoomId
                && GameEngineData.SequenceEqual(other.GameEngineData)
                && MatchmakerData.SequenceEqual(other.MatchmakerData);
        }

        public override int GetHashCode() => HashCode.Combine(RoomId, GameEngineData.Length, MatchmakerData.Length);
    }
}