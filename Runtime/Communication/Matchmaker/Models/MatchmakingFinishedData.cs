using System;
using System.Linq;
using Elympics.Models.Matchmaking.LongPolling;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics.Models.Matchmaking
{
    public class MatchmakingFinishedData : IEquatable<MatchmakingFinishedData?>
    {
        public Guid MatchId { get; }
        public string UserSecret { get; }
        public string QueueName { get; }
        public string RegionName { get; }
        public byte[] GameEngineData { get; }
        public float[] MatchmakerData { get; }
        public string TcpUdpServerAddress { get; }
        public string WebServerAddress { get; }
        public Guid[] MatchedPlayers { get; }

        internal MatchmakingFinishedData(Guid matchId, string userSecret, string queueName, string regionName,
            byte[] gameEngineData, float[] matchmakerData, string tcpUdpServerAddress, string webServerAddress,
            Guid[] matchedPlayers)
        {
            MatchId = matchId;
            UserSecret = userSecret;
            QueueName = queueName;
            RegionName = regionName;
            GameEngineData = gameEngineData;
            MatchmakerData = matchmakerData;
            TcpUdpServerAddress = tcpUdpServerAddress;
            WebServerAddress = webServerAddress;
            MatchedPlayers = matchedPlayers;
        }

        internal MatchmakingFinishedData(WebSocket.MatchData matchData)
        {
            MatchId = matchData.MatchId;
            UserSecret = matchData.UserSecret;
            QueueName = matchData.QueueName;
            RegionName = matchData.RegionName;
            GameEngineData = matchData.GameEngineData;
            MatchmakerData = matchData.MatchmakerData;
            TcpUdpServerAddress = matchData.TcpUdpServerAddress;
            WebServerAddress = matchData.WebServerAddress;
            MatchedPlayers = matchData.MatchedPlayersId;
        }

        internal MatchmakingFinishedData(GetMatchModel.Response matchResponse)
        {
            var matchmakerData = matchResponse.UserData?.MatchmakerData ?? Array.Empty<float>();
            var gameEngineData = Convert.FromBase64String(matchResponse.UserData?.GameEngineData ?? "");

            MatchId = new Guid(matchResponse.MatchId);
            UserSecret = matchResponse.UserSecret;
            QueueName = matchResponse.QueueName;
            RegionName = matchResponse.RegionName;
            GameEngineData = gameEngineData;
            MatchmakerData = matchmakerData;
            TcpUdpServerAddress = matchResponse.TcpUdpServerAddress;
            WebServerAddress = matchResponse.WebServerAddress;
            MatchedPlayers = matchResponse.MatchedPlayersId.Select(x => new Guid(x)).ToArray();
        }

        public MatchmakingFinishedData(Guid matchId, MatchDetails matchData, string queueName, string regionName)
        {
            MatchId = matchId;
            UserSecret = matchData.UserSecret;
            QueueName = queueName;
            RegionName = regionName;
            GameEngineData = matchData.GameEngineData;
            MatchmakerData = matchData.MatchmakerData;
            TcpUdpServerAddress = matchData.TcpUdpServerAddress;
            WebServerAddress = matchData.WebServerAddress;
            MatchedPlayers = matchData.MatchedPlayersId.ToArray();
        }

        public bool Equals(MatchmakingFinishedData? other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return MatchId.Equals(other.MatchId)
                && UserSecret == other.UserSecret
                && QueueName == other.QueueName
                && RegionName == other.RegionName
                && GameEngineData.SequenceEqual(other.GameEngineData)
                && MatchmakerData.SequenceEqual(other.MatchmakerData)
                && TcpUdpServerAddress == other.TcpUdpServerAddress
                && WebServerAddress == other.WebServerAddress
                && MatchedPlayers.SequenceEqual(other.MatchedPlayers);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj.GetType() == GetType() && Equals((MatchmakingFinishedData)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(MatchId);
            hashCode.Add(UserSecret);
            hashCode.Add(QueueName);
            hashCode.Add(RegionName);
            hashCode.Add(GameEngineData.Length);
            hashCode.Add(MatchmakerData.Length);
            hashCode.Add(TcpUdpServerAddress);
            hashCode.Add(WebServerAddress);
            hashCode.Add(MatchedPlayers.Length);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(MatchmakingFinishedData? left, MatchmakingFinishedData? right) => Equals(left, right);
        public static bool operator !=(MatchmakingFinishedData? left, MatchmakingFinishedData? right) => !Equals(left, right);
    }
}
