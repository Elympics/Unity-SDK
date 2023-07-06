using System;

namespace MatchTcpModels.Messages
{
    [Serializable]
    public class PingClientResponseMessage : Message
    {
        public string NtpData;

        public PingClientResponseMessage()
        {
            Type = MessageType.PingClientResponse;
        }
    }
}
