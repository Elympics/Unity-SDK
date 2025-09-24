using System;
using System.Collections.Generic;
using Elympics.Util;
using Elympics.Communication.Authentication.Models;

#nullable enable

namespace Elympics
{
    public class UserInfo : IEquatable<UserInfo>
    {
        public readonly uint? TeamIndex;
        public readonly bool IsReady;
        public readonly Dictionary<string, string> CustomPlayerData;
        public readonly ElympicsUser User;

        [Obsolete("Use " + nameof(User) + "." + nameof(ElympicsUser.Nickname) + " instead.")]
        public string Nickname => User.Nickname;
        [Obsolete("Use " + nameof(User) + "." + nameof(ElympicsUser.AvatarUrl) + " instead.")]
        public string AvatarUrl => User.AvatarUrl;
        [Obsolete("Use " + nameof(User) + "." + nameof(ElympicsUser.UserId) + " instead.")]
        public Guid UserId => User.UserId;

        public UserInfo(uint? teamIndex, bool isReady, Dictionary<string, string> customPlayerData, ElympicsUser user)
        {
            TeamIndex = teamIndex;
            IsReady = isReady;
            CustomPlayerData = customPlayerData;
            User = user;
        }

        public bool Equals(UserInfo? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return TeamIndex == other.TeamIndex && IsReady == other.IsReady && StringIReadOnlyDictionaryEqualityComparer.Instance.Equals(CustomPlayerData, other.CustomPlayerData) && User.Equals(other.User);
        }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is UserInfo other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(TeamIndex, IsReady, User);

        public static bool operator ==(UserInfo? left, UserInfo? right) => Equals(left, right);

        public static bool operator !=(UserInfo? left, UserInfo? right) => !Equals(left, right);
    }
}
