#nullable enable

namespace GameBotCore.V1._3
{
    public class BotConfiguration : GameBotCore.V1._2.BotConfiguration
    {
        public byte[]? GameEngineData { get; set; }

        public float[]? MatchmakerData { get; set; }
    }
}
