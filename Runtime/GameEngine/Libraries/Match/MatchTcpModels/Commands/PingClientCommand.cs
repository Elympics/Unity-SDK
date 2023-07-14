using System;

namespace MatchTcpModels.Commands
{
    [Serializable]
    public class PingClientCommand : Command
    {
        public string NtpData;

        public PingClientCommand() : base(CommandType.PingClient)
        {
        }
    }
}
