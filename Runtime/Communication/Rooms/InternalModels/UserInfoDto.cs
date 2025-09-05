using Elympics.Communication.Authentication.Models.Internal;
using System.Collections.Generic;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels
{
    [MessagePackObject]
    public record UserInfoDto
    {
        [Key(1)] public uint? TeamIndex;
        [Key(2)] public bool IsReady;
        [Key(5)] public Dictionary<string, string> CustomPlayerData;
        [Key(6)] public ElympicsUserDTO User;

        public UserInfoDto() { }
        public UserInfoDto(uint? teamIndex, bool isReady, Dictionary<string, string> customPlayerData, ElympicsUserDTO user)
        {
            TeamIndex = teamIndex;
            IsReady = isReady;
            CustomPlayerData = customPlayerData;
            User = user;
        }

        public UserInfo ToPublicModel() => new(TeamIndex, IsReady, CustomPlayerData, User.ToPublicModel());
    }
}
