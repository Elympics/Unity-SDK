using System;
using MessagePack;

#nullable enable

namespace Elympics
{
    [MessagePackObject]
    public record UserInfo(
        [property: Key(0)] Guid UserId,
        [property: Key(1)] uint? TeamIndex,
        [property: Key(2)] bool IsReady);

}
