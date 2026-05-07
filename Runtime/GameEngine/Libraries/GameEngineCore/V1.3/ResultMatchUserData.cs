#nullable enable

namespace GameEngineCore.V1._3
{
    public class ResultMatchUserData
    {
        public string? UserId { get; set; }
        public byte[]? GameEngineData { get; set; }
        public float[]? MatchmakerData { get; set; }
    }
}
