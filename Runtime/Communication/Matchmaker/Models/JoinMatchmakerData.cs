using System;

namespace Elympics.Models.Matchmaking
{
    public struct JoinMatchmakerData
    {
        public Guid GameId { get; set; }
        public string GameVersion { get; set; }
        public string QueueName { get; set; }
        public string RegionName { get; set; }
        public byte[] GameEngineData { get; set; }
        public float[] MatchmakerData { get; set; }
    }
}
