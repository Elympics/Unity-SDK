#nullable enable

namespace GameEngineCore.V1._3
{
    public class InitialMatchUserData
    {
        public string? UserId { get; set; }
        public bool IsBot { get; set; }
        public double BotDifficulty { get; set; }
        public byte[]? GameEngineData { get; set; }
        public float[]? MatchmakerData { get; set; }
    }
}
