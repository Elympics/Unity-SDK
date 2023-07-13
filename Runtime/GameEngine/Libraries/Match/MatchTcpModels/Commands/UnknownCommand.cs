using System;

namespace MatchTcpModels.Commands
{
    [Serializable]
    public class UnknownCommand : Command
    {
        public UnknownCommand() : base(CommandType.Unknown)
        {
        }
    }
}
