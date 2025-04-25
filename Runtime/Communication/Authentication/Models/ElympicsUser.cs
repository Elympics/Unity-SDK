#nullable enable

using System;
namespace Elympics.Communication.Authentication.Models
{
    public readonly struct ElympicsUser
    {
        public Guid UserId { get; init; }
        public string? Nickname { get; init; }
        public string? AvatarUrl { get; init; }
    }
}
