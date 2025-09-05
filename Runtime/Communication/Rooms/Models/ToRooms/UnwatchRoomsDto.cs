using System;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record UnwatchRoomsDto() : LobbyOperation
    {
        [SerializationConstructor]
        public UnwatchRoomsDto(Guid operationId) : this() =>
            OperationId = operationId;
    }
}
