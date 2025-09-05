using System;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Lobby.Models.ToLobby
{
    [MessagePackObject]
    public record ShowAuthDto() : LobbyOperation
    {
        [SerializationConstructor]
        public ShowAuthDto(Guid operationId) : this() => OperationId = operationId;
    }
}
