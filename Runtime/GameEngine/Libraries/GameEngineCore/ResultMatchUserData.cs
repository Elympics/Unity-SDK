#nullable enable

namespace GameEngineCore
{
    public class ResultMatchUserData
    {
        public string? UserId { get; set; }
        public byte[]? GameEngineData { get; set; }
        public float[]? MatchmakerData { get; set; }
    }
}
