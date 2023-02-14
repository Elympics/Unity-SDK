using System;

namespace MatchTcpModels.Commands
{
	[Serializable]
	public class NoneCommand : Command
	{
		public NoneCommand() : base(CommandType.None)
		{
		}
	}
}
