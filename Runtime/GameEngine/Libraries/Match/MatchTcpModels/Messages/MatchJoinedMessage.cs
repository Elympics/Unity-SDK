using System;

namespace MatchTcpModels.Messages
{
	[Serializable]
	public class MatchJoinedMessage : Message
	{
		public string MatchId;

		public MatchJoinedMessage()
		{
			Type = MessageType.MatchJoined;
		}
	}
}
