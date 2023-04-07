using System;
using Elympics.Models.Matchmaking.WebSocket;

namespace Elympics.Models.Matchmaking
{
	public class MatchmakingFinishedData
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

		public MatchmakingFinishedData(Guid matchId, string userSecret, string queueName, string regionName,
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

		public MatchmakingFinishedData(MatchData matchData)
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
	}
}
