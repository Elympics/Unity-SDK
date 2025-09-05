using System;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels.ToRooms
{
    [MessagePackObject]
    public record UnwatchRoomsDto() : LobbyOperation
    {
        [SerializationConstructor]
        public UnwatchRoomsDto(Guid operationId) : this() =>
            OperationId = operationId;
    }
}
