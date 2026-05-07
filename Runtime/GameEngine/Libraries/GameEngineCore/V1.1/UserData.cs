#nullable enable
using System;

namespace GameEngineCore.V1._1
{
    [Obsolete("Use newer version from V1.3")]
    public class UserData
    {
        public const int BlankRankingAfterValue = int.MinValue;
        public string? UserId { get; set; }
        public int RankingBefore { get; set; }
        public int RankingAfter { get; set; } = BlankRankingAfterValue;
        public int[]? MatchmakerData { get; set; }
        public byte[]? GameEngineData { get; set; }
    }
    // For initial data, eg. Legendary - array of heroes indexes: 1 when hero present in the user's deck
    // For result data, eg Legendary - array: first field induces whether IsWinner, next ones contain information about units health left
}
