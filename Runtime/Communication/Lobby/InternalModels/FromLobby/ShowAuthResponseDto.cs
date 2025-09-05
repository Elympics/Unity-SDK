using System;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Lobby.InternalModels.FromLobby
{
    [MessagePackObject]
    public record ShowAuthResponseDto(
        [property: Key(0)] Guid UserId,
        [property: Key(1)] string AuthType,
        [property: Key(2)] string? EthAddress,
        [property: Key(3)] string? Nickname,
        [property: Key(4)] string? AvatarUrl,
        [property: Key(5)] Guid RequestId) : ILobbyResponse;
}
