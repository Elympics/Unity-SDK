using MessagePack;

namespace Elympics.Models.Matchmaking.WebSocket
{
    [MessagePackObject]
    public readonly struct JoinMatchmaker : IToMatchmaker
    {
        [Key(0)] public string QueueName { get; }
        [Key(1)] public string RegionName { get; }
        [Key(2)] public byte[] GameEngineData { get; }
        [Key(3)] public float[] MatchmakerData { get; }

        public JoinMatchmaker(string queueName, string regionName, byte[] gameEngineData, float[] matchmakerData)
        {
            QueueName = queueName;
            RegionName = regionName;
            GameEngineData = gameEngineData;
            MatchmakerData = matchmakerData;
        }

        internal JoinMatchmaker(JoinMatchmakerData joinMatchmakerData)
        {
            QueueName = joinMatchmakerData.QueueName;
            RegionName = joinMatchmakerData.RegionName;
            GameEngineData = joinMatchmakerData.GameEngineData;
            MatchmakerData = joinMatchmakerData.MatchmakerData;
        }
    }
}
