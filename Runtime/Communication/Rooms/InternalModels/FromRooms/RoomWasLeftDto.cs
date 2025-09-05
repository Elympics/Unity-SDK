using System;
using Elympics.Communication.Rooms.Models;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record RoomWasLeftDto(
        [property: Key(0)] Guid RoomId,
        [property: Key(1)] LeavingReasonDto Reason) : IFromLobby;
}
