using System;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record WatchRoomsDto() : LobbyOperation
    {
        [SerializationConstructor]
        public WatchRoomsDto(Guid operationId) : this() =>
            OperationId = operationId;
    }
}
