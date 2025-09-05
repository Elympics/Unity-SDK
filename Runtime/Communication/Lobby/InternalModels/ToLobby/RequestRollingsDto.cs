using System;
using System.Collections.Generic;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Lobby.InternalModels.ToLobby
{
    [MessagePackObject]
    public record RequestRollingsDto(
        [property: Key(1)] Guid GameId,
        [property: Key(2)] string VersionId,
        [property: Key(3)] List<RollingRequestDto> Rollings
        ) : LobbyOperation
    {
        [SerializationConstructor]
        public RequestRollingsDto(Guid operationId, Guid GameId, string VersionId, List<RollingRequestDto> Rollings) : this(GameId, VersionId, Rollings) => OperationId = operationId;
    }
}
