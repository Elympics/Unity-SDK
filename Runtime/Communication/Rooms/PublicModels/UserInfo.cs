using System;
using System.Collections.Generic;

#nullable enable

namespace Elympics
{
    public record UserInfo(Guid UserId, uint? TeamIndex, bool IsReady, string? Nickname, string? AvatarUrl, Dictionary<string, string> CustomPlayerData);
}
