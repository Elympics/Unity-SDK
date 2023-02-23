using System;

namespace MatchTcpModels.Commands
{
	[Serializable]
	public class InGameDataCommand : Command
	{
		public string Data;

		public InGameDataCommand() : base(CommandType.InGameData)
		{
		}
	}
}
