using System;
using System.Collections.Generic;
using MessagePack;

#nullable enable

namespace Elympics
{
    [MessagePackObject]
    public record UserInfo(
        [property: Key(0)] Guid UserId,
        [property: Key(1)] uint? TeamIndex,
        [property: Key(2)] bool IsReady,
        [property: Key(3)] string? Nickname,
        [property: Key(4)] string? AvatarUrl,
        [property: Key(5)] Dictionary<string, string> CustomPlayerData);
}
