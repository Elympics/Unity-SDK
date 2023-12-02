using System;
using GameEngineCore.V1._4;

#nullable enable

namespace Elympics
{
    public class InitialMatchPlayerDataGuid
    {
        public ElympicsPlayer Player { get; set; }
        public Guid UserId { get; set; }
        public bool IsBot { get; set; }
        public double BotDifficulty { get; set; }
        public byte[] GameEngineData { get; set; }
        public float[] MatchmakerData { get; set; }
        public Guid? RoomId { get; set; }
        public uint? TeamIndex { get; set; }

        internal InitialMatchPlayerDataGuid()
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
        }
    }
}
