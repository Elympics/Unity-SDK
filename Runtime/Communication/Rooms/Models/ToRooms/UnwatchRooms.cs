using System;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record UnwatchRooms() : LobbyOperation
    {
        [SerializationConstructor]
        public UnwatchRooms(Guid operationId) : this() =>
            OperationId = operationId;
    }
}
