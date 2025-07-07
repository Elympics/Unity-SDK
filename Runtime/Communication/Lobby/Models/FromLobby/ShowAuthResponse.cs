#nullable enable
using System;
using Communication.Lobby.Models.FromLobby;
using MessagePack;

namespace Elympics.Communication.Lobby.Models.FromLobby
{
    [MessagePackObject]
    public record ShowAuthResponse(
        [property: Key(0)] Guid UserId,
        [property: Key(1)] string AuthType,
        [property: Key(2)] string? EthAddress,
        [property: Key(3)] string? Nickname,
        [property: Key(4)] string? AvatarUrl,
        [property: Key(5)] Guid RequestId) : IDataFromLobby;
}
