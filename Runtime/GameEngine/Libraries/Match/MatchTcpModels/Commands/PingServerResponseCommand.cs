using System;

namespace MatchTcpModels.Commands
{
    [Serializable]
    public class PingServerResponseCommand : Command
    {
        public PingServerResponseCommand() : base(CommandType.PingServerResponse)
        {
        }
    }
}
