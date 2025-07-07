using System;
using System.Collections.Generic;
using Communication.Lobby.Models.FromLobby;
using MessagePack;

#nullable enable
namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record GameDataResponse(
        [property: Key(0)] int JoinedMatchRooms,
        [property: Key(1)] List<RoomCoin> CoinData,
        [property: Key(2)] string GameVersionId,
        [property: Key(3)] string FleetName,
        [property: Key(4)] Guid RequestId) : IDataFromLobby
    {
        public virtual bool Equals(GameDataResponse? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return JoinedMatchRooms == other.JoinedMatchRooms && GameVersionId == other.GameVersionId && FleetName == other.FleetName;
        }

        public override int GetHashCode() => JoinedMatchRooms.GetHashCode();

        public override string ToString() =>
            $"RoomsJoined: {JoinedMatchRooms} | {nameof(GameVersionId)}:{GameVersionId} | {nameof(FleetName)}:{FleetName} | Coins count: {(CoinData != null ? CoinData.Count : "No coins found.")}";
    }
}
