using System;
using MessagePack;

namespace Elympics.Models.Matchmaking.WebSocket
{
    [MessagePackObject]
    public readonly struct MatchData : IFromMatchmaker
    {
        [Key(0)] public Guid MatchId { get; }
        [Key(1)] public string UserSecret { get; }
        [Key(2)] public string QueueName { get; }
        [Key(3)] public string RegionName { get; }
        [Key(4)] public byte[] GameEngineData { get; }
        [Key(5)] public float[] MatchmakerData { get; }
        [Key(6)] public string TcpUdpServerAddress { get; }
        [Key(7)] public string WebServerAddress { get; }
        [Key(8)] public Guid[] MatchedPlayersId { get; }

        public MatchData(Guid matchId, string userSecret, string queueName, string regionName, byte[] gameEngineData,
            float[] matchmakerData, string tcpUdpServerAddress, string webServerAddress, Guid[] matchedPlayersId)
        {
            MatchId = matchId;
            UserSecret = userSecret;
            QueueName = queueName;
            RegionName = regionName;
            GameEngineData = gameEngineData;
            MatchmakerData = matchmakerData;
            TcpUdpServerAddress = tcpUdpServerAddress;
            WebServerAddress = webServerAddress;
            MatchedPlayersId = matchedPlayersId;
        }
    }
}
