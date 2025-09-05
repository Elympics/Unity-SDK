#nullable enable

using System;
using System.Collections.Generic;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using MessagePack;

namespace Elympics.Communication.Rooms.InternalModels.ToRooms
{
    [MessagePackObject]
    public record UpdateCustomPlayerDataDto(
        [property: Key(1)] Guid RoomId,
        [property: Key(2)] Dictionary<string, string> CustomPlayerData) : LobbyOperation
    {
        [SerializationConstructor]
        public UpdateCustomPlayerDataDto(Guid operationId, Guid roomId, Dictionary<string, string> customPlayerData) : this(roomId, customPlayerData) =>
            OperationId = operationId;
    }
}
