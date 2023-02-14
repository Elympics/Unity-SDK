using System;

namespace MatchTcpModels.Commands
{
	[Serializable]
	public class Command
	{
		public CommandType Type;

		public Command()
		{
		}

		protected Command(CommandType type)
		{
			Type = type;
		}
	}
}
