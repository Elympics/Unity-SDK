using System.Collections.Generic;
using System.Linq;

namespace Elympics.Models.Matchmaking
{
	public class JoinedMatchData
	{
		public JoinedMatchData(MatchmakingFinishedData matchmakingFinishedData)
		{
			MatchId = matchmakingFinishedData.MatchId.ToString();
			TcpUdpServerAddress = matchmakingFinishedData.TcpUdpServerAddress;
			WebServerAddress = matchmakingFinishedData.WebServerAddress;
			UserSecret = matchmakingFinishedData.UserSecret;
			MatchedPlayers = matchmakingFinishedData.MatchedPlayers.Select(x => x.ToString()).ToList();
			MatchmakerData = matchmakingFinishedData.MatchmakerData;
			GameEngineData = matchmakingFinishedData.GameEngineData;
			QueueName = matchmakingFinishedData.QueueName;
			RegionName = matchmakingFinishedData.RegionName;
		}

		public string       MatchId             { get; }
		public string       TcpUdpServerAddress { get; }
		public string       WebServerAddress    { get; }
		public string       UserSecret          { get; }
		public List<string> MatchedPlayers      { get; }
		public float[]      MatchmakerData      { get; }
		public byte[]       GameEngineData      { get; }
		public string       QueueName           { get; }
		public string       RegionName          { get; }
	}
}
