#nullable enable

using System;

namespace Elympics.Communication.Authentication.Models
{
    /// <summary>Represents a user's public profile.</summary>
    public readonly struct ElympicsUser : IEquatable<ElympicsUser>
    {
        /// <summary>Globally-unique user ID.</summary>
        public readonly Guid UserId;
        /// <summary>User's current nickname.</summary>
        public readonly string Nickname;
        /// <summary>Current status of user's <see cref="Nickname"/>.</summary>
        public readonly NicknameType NicknameType;
        /// <summary>URL from which user's avatar image can be fetched.</summary>
        public readonly string AvatarUrl;

        public ElympicsUser(Guid userId, string nickname, NicknameType nicknameType, string avatarUrl)
        {
            UserId = userId;
            Nickname = nickname;
            NicknameType = nicknameType;
            AvatarUrl = avatarUrl;
        }

        public bool Equals(ElympicsUser other) => UserId.Equals(other.UserId) && Nickname == other.Nickname && NicknameType == other.NicknameType && AvatarUrl == other.AvatarUrl;

        public override bool Equals(object? obj) => obj is ElympicsUser other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(UserId, Nickname, (int)NicknameType, AvatarUrl);

        public static bool operator ==(ElympicsUser left, ElympicsUser right) => left.Equals(right);

        public static bool operator !=(ElympicsUser left, ElympicsUser right) => !left.Equals(right);
    }

    public enum NicknameType
    {
        /// <summary>Unexpected and invalid value that indicates an error in the Elympics systems.</summary>
        Undefined = 0,
        /// <summary>Regular, non-verified username.</summary>
        Common = 1,
        /// <summary>Special status for users who have verified their username.</summary>
        Verified = 2,
    }
}
