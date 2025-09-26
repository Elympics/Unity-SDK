using System;
using System.Collections.Generic;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Lobby.InternalModels.FromLobby
{
    [MessagePackObject]
    public record RollingsResponseDto(
        [property: Key(0)] List<RollingResponseDto> Rollings,
        [property: Key(1)] Guid RequestId) : ILobbyResponse;
}
