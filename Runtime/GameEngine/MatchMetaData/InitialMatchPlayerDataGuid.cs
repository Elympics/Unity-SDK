using System;
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

        public InitialMatchPlayerDataGuid()
        {

        }
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
        }

        public InitialMatchPlayerDataGuid(ElympicsPlayer player, byte[] gameEngineData, float[] matchmakerData)
        {
            Player = player;
            GameEngineData = gameEngineData;
            MatchmakerData = matchmakerData;
        }
    }
}
