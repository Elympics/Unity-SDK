using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Communication.Authentication.Models.Internal;
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

        public override string ToString() => $"{nameof(User)}:{Environment.NewLine}\t{User}{Environment.NewLine}"
            + $"{nameof(TeamIndex)}:{TeamIndex}{Environment.NewLine}"
            + $"{nameof(IsReady)}:{IsReady}{Environment.NewLine}"
            + $"{nameof(CustomPlayerData)}:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", CustomPlayerData?.Select(kv => $"Key = {kv.Key}, Value = {kv.Value}"))}{Environment.NewLine}";
    }
}
