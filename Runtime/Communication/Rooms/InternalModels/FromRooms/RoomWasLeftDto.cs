using System;
using Elympics.Communication.Lobby.InternalModels.FromLobby;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels.FromRooms
{
    [MessagePackObject]
    public record RoomWasLeftDto(
        [property: Key(0)] Guid RoomId,
        [property: Key(1)] LeavingReasonDto Reason) : IFromLobby;
}
