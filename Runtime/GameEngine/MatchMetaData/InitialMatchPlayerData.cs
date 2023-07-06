using System;

namespace Elympics
{
    [Obsolete("Use " + nameof(InitialMatchPlayerDataGuid) + " instead")]
    public class InitialMatchPlayerData
    {
        public ElympicsPlayer Player { get; set; }
        public string UserId { get; set; }
        public bool IsBot { get; set; }
        public double BotDifficulty { get; set; }
        public byte[] GameEngineData { get; set; }
        public float[] MatchmakerData { get; set; }

        public InitialMatchPlayerData()
        { }

        public InitialMatchPlayerData(InitialMatchPlayerDataGuid initialMatchPlayerDataGuid)
        {
            Player = initialMatchPlayerDataGuid.Player;
            UserId = initialMatchPlayerDataGuid.UserId.ToString();
            IsBot = initialMatchPlayerDataGuid.IsBot;
            BotDifficulty = initialMatchPlayerDataGuid.BotDifficulty;
            GameEngineData = initialMatchPlayerDataGuid.GameEngineData;
            MatchmakerData = initialMatchPlayerDataGuid.MatchmakerData;
        }
    }
}
