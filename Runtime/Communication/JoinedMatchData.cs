using System.Collections.Generic;

namespace Elympics
{
	public class JoinedMatchData
	{
		public JoinedMatchData(string matchId, string tcpUdpServerAddress, string webServerAddress, string userSecret, List<string> matchedPlayers, float[] matchmakerData, byte[] gameEngineData)
		{
			MatchId = matchId;
			TcpUdpServerAddress = tcpUdpServerAddress;
			WebServerAddress = webServerAddress;
			UserSecret = userSecret;
			MatchedPlayers = matchedPlayers;
			MatchmakerData = matchmakerData;
			GameEngineData = gameEngineData;
		}

		public string       MatchId             { get; }
		public string       TcpUdpServerAddress { get; }
		public string       WebServerAddress    { get; }
		public string       UserSecret          { get; }
		public List<string> MatchedPlayers      { get; }
		public float[]      MatchmakerData      { get; }
		public byte[]       GameEngineData      { get; }
	}
}
