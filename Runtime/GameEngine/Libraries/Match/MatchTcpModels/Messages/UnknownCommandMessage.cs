using System;

namespace MatchTcpModels.Messages
{
	[Serializable]
	public class UnknownCommandMessage : Message
	{
		public UnknownCommandMessage()
		{
			Type = MessageType.UnknownCommandMessage;
		}
	}
}
