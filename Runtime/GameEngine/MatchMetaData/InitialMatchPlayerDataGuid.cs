using System;
using System.Collections.Generic;
using Elympics.Communication.Authentication.Models;
using GameEngineCore.V1._4;
using MessagePack;

#nullable enable

namespace Elympics
{
    [MessagePackObject]
    public class InitialMatchPlayerDataGuid
    {
        /// <summary>In-match player identifier.</summary>
        [Key(0)] public ElympicsPlayer Player { get; set; }

        /// <summary>Globally unique player identifier.</summary>
        [Key(1)] public Guid UserId { get; set; }

        [Key(2)] public bool IsBot { get; set; }
        [Key(3)] public double BotDifficulty { get; set; }

        /// <summary>Optional game-specific data which can be used to provide initial settings for a match.</summary>
        [Key(4)] public byte[] GameEngineData { get; set; }

        [Key(5)] public float[] MatchmakerData { get; set; }
        [Key(6)] public Guid? RoomId { get; set; }
        [Key(7)] public uint? TeamIndex { get; set; }
        [Key(8)] public string? TelegramId { get; set; }
        [Key(9)] public string? Address { get; set; }
        [Key(10)] public string? Nickname { get; set; }
        [Key(11)] public NicknameType NicknameType { get; set; }

        [Key(12)] public IReadOnlyDictionary<string, string>? CustomData { get; set; }

        public InitialMatchPlayerDataGuid()
        { }
        internal InitialMatchPlayerDataGuid(ElympicsPlayer player, InitialMatchUserData userData)
        {
            Player = player;
            UserId = userData.UserId;
            IsBot = userData.IsBot;
            BotDifficulty = userData.BotDifficulty;
            GameEngineData = userData.GameEngineData;
            MatchmakerData = userData.MatchmakerData;
            RoomId = userData.RoomId;
            TeamIndex = userData.TeamIndex;
            TelegramId = userData.Telegramid;
            Address = userData.Address;
            Nickname = userData.Nickname;
            NicknameType = ConvertToNickNameType(userData.NicknameType);
            CustomData = userData.CustomData;
        }

        public static NicknameType ConvertToNickNameType(string? type) => type switch
        {
            "Common" => NicknameType.Common,
            "Verified" => NicknameType.Verified,
            _ => NicknameType.Undefined
        };

        public InitialMatchPlayerDataGuid(ElympicsPlayer player, byte[] gameEngineData, float[] matchmakerData)
        {
            Player = player;
            GameEngineData = gameEngineData;
            MatchmakerData = matchmakerData;
        }
    }
}
