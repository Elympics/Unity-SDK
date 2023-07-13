using System;

namespace MatchTcpModels.Commands
{
    [Serializable]
    public class JoinMatchCommand : Command
    {
        public JoinMatchCommand() : base(CommandType.JoinMatch)
        {
        }
    }
}
