#nullable enable

using System;
using MessagePack;

namespace Elympics.Communication.Lobby.InternalModels.ToLobby
{
    [MessagePackObject]
    public record ShowAuthDto() : LobbyOperation
    {
        [SerializationConstructor]
        public ShowAuthDto(Guid operationId) : this() => OperationId = operationId;
    }
}
