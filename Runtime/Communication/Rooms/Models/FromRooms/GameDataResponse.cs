using Elympics.Lobby.Models;
using MessagePack;

#nullable enable
namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record GameDataResponse([property: Key(0)] int JoinedMatchRooms) : IFromLobby
    {
        public virtual bool Equals(GameDataResponse? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return JoinedMatchRooms == other.JoinedMatchRooms;
        }

        public override int GetHashCode() => JoinedMatchRooms.GetHashCode();

        public override string ToString() => $"RoomsJoined: {JoinedMatchRooms}";
    }
}
