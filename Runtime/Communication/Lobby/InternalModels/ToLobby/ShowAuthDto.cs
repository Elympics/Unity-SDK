using System;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Lobby.InternalModels.ToLobby
{
    [MessagePackObject]
    public record ShowAuthDto() : LobbyOperation
    {
        [SerializationConstructor]
        public ShowAuthDto(Guid operationId) : this() => OperationId = operationId;
    }
}
