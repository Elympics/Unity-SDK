using System;
using Elympics.Lobby.Models;
using MessagePack;

namespace Elympics.Communication.Lobby.Models.ToLobby
{
    [MessagePackObject]
    public record ShowAuth() : LobbyOperation
    {
        [SerializationConstructor]
        public ShowAuth(Guid operationId) : this() => OperationId = operationId;
    }
}
