using System;
using System.Collections.Generic;
using Elympics.Communication.Lobby.Models.FromLobby;
using MessagePack;

#nullable enable

namespace Communication.Lobby.Models.ToLobby
{
    [MessagePackObject]
    public record RollingsResponseDto(
        [property: Key(0)] List<RollingResponseDto> Rollings,
        [property: Key(1)] Guid RequestId) : ILobbyResponse;
}
