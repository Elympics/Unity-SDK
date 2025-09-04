#nullable enable

using System;
namespace Elympics.Communication.Authentication.Models
{
    public readonly struct ElympicsUser
    {
        /// <summary>Globally-unique user ID.</summary>
        public readonly Guid UserId;
        /// <summary>User's current nickname.</summary>
        public readonly string Nickname;
        /// <summary>Current status of user's <see cref="Nickname"/>.</summary>
        public readonly NicknameStatus NicknameStatus;
        /// <summary>URL from which user's avatar image can be fetched.</summary>
        public readonly string AvatarUrl;

        public ElympicsUser(Guid userId, string nickname, NicknameStatus nicknameStatus, string avatarUrl)
        {
            UserId = userId;
            Nickname = nickname;
            NicknameStatus = nicknameStatus;
            AvatarUrl = avatarUrl;
        }
    }

    public enum NicknameStatus
    {
        /// <summary>Unexpected and invalid value that indicates an error in the Elympics systems.</summary>
        Unknown = 0,
        /// <summary>Regular, non-verified username.</summary>
        NotVerified = 1,
        /// <summary>Special status for users who have verified their username.</summary>
        Verified = 2,
    }
}
