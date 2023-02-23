using System;

namespace MatchTcpModels.Commands
{
	[Serializable]
	public class AuthenticateAsSpectatorCommand : Command
	{
		public AuthenticateAsSpectatorCommand() : base(CommandType.AuthenticateAsSpectator)
		{
		}
	}
}
