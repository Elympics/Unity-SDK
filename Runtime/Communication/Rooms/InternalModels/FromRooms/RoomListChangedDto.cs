using System.Collections.Generic;
using System.Linq;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record RoomListChangedDto([property: Key(0)] IReadOnlyList<ListedRoomChange> Changes) : IFromLobby
    {
        public virtual bool Equals(RoomListChangedDto? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Changes.SequenceEqual(other.Changes);
        }

        public override int GetHashCode() => Changes.Count.GetHashCode();
    }
}
