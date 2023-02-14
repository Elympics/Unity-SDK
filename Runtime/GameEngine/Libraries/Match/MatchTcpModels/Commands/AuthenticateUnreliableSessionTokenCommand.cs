using System;

namespace MatchTcpModels.Commands
{
	[Serializable]
	public class AuthenticateUnreliableSessionTokenCommand : Command
	{
		public string SessionToken;

		public AuthenticateUnreliableSessionTokenCommand() : base(CommandType.AuthenticateUnreliableSessionToken)
		{
		}
	}
}
