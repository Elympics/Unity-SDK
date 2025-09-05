using System;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record ListedRoomChange(
        [property: Key(0)] Guid RoomId,
        [property: Key(1)] PublicRoomState? PublicRoomState);

}
