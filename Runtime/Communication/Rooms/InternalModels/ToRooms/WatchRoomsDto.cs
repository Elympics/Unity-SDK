using System;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels.ToRooms
{
    [MessagePackObject]
    public record WatchRoomsDto() : LobbyOperation
    {
        [SerializationConstructor]
        public WatchRoomsDto(Guid operationId) : this() =>
            OperationId = operationId;
    }
}
