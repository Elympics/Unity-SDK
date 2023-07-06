using System;

namespace Elympics.Models.Matchmaking.LongPolling
{
    [Serializable]
    public class UserOrBotData
    {
        public string UserId;
        public bool IsBot;
        public double BotDifficulty;
        public string GameEngineData; // Has to explicitly convert from base64 string
        public float[] MatchmakerData;
    }
}
