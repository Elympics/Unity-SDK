using System;

namespace MatchTcpModels.Messages
{
    [Serializable]
    public class PingServerMessage : Message
    {
        public PingServerMessage()
        {
            Type = MessageType.PingServer;
        }
    }
}
