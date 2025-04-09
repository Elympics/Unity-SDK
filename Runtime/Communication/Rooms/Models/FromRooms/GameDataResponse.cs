using System.Collections.Generic;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable
namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record GameDataResponse(
        [property: Key(0)] int JoinedMatchRooms,
        [property: Key(1)] List<RoomCoin> CoinData,
        [property: Key(2)] string GameVersionId,
        [property: Key(3)] string FleetName) : IFromLobby
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

        public override string ToString() => $"RoomsJoined: {JoinedMatchRooms}";
    }
}
