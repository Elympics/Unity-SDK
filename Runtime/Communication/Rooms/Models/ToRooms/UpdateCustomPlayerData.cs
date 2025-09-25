#nullable enable

using System;
using System.Collections.Generic;
using Elympics.Lobby.Models;
using MessagePack;

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record UpdateCustomPlayerData(
        [property: Key(1)] Guid RoomId,
        [property: Key(2)] Dictionary<string, string> CustomPlayerData) : LobbyOperation
    {
        [SerializationConstructor]
        public UpdateCustomPlayerData(Guid operationId, Guid roomId, Dictionary<string, string> customPlayerData) : this(roomId, customPlayerData) =>
            OperationId = operationId;
    }
}
