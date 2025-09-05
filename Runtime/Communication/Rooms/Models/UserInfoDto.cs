using System;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.Models
{
    [MessagePackObject]
    public record UserInfoDto(
        [property: Key(0)] Guid UserId,
        [property: Key(1)] uint? TeamIndex,
        [property: Key(2)] bool IsReady,
        [property: Key(3)] string? Nickname,
        [property: Key(4)] string? AvatarUrl);
}
