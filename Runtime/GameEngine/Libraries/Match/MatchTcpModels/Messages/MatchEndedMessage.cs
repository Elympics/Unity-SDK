using System;

namespace MatchTcpModels.Messages
{
	[Serializable]
	public class MatchEndedMessage : Message
	{
		public string MatchId;

		public MatchEndedMessage()
		{
			Type = MessageType.MatchEnded;
		}
	}
}
