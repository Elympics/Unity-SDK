#nullable enable

using System;
using MessagePack;

namespace Elympics.Communication.Authentication.Models.Internal
{
    /// <summary>Internal version of <see cref="ElympicsUser"/> used for communication with backend and PlayPad.</summary>
    /// <remarks>This is the standard user data model which should always be used when sending data about a user to a game client.</remarks>
    [Serializable, MessagePackObject]
    public struct ElympicsUserDTO
    {
        /// <summary>Globally-unique user ID.</summary>
        [Key(0)] public string userId;
        /// <summary>User's current nickname.</summary>
        [Key(1)] public string nickname;
        /// <summary>Current status of user's <see cref="nickname"/>.</summary>
        [Key(2)] public int nicknameType;
        /// <summary>URL from which user's avatar image can be fetched.</summary>
        [Key(3)] public string avatarUrl;

        public ElympicsUserDTO(string userId, string nickname, int nicknameType, string avatarUrl)
        {
            this.userId = userId;
            this.nickname = nickname;
            this.nicknameType = nicknameType;
            this.avatarUrl = avatarUrl;
        }

        public ElympicsUser ToPublicModel()
        {
            if (!Guid.TryParse(userId, out var userGuid))
                throw new ElympicsException($"{nameof(ElympicsUserDTO)}.{nameof(ToPublicModel)} failed, because provided {nameof(userId)} is not a valid GUID string: "
                                            + $"\"{userId}\". This exception is most likely caused by invalid data sent by PlayPad or Elympics backend.");

            var nickStatus = nicknameType switch
            {
                1 => NicknameType.Common,
                2 => NicknameType.Verified,
                _ => NicknameType.Undefined
            };

            if (nickStatus == NicknameType.Undefined)
                ElympicsLogger.LogError($"Unexpected {nameof(nicknameType)} received: {nicknameType}.");

            return new ElympicsUser(userGuid, nickname, nickStatus, avatarUrl);
        }
    }
}
