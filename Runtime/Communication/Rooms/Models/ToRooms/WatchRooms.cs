using System;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record WatchRooms() : LobbyOperation
    {
        [SerializationConstructor]
        public WatchRooms(Guid operationId) : this() =>
            OperationId = operationId;
    }
}
