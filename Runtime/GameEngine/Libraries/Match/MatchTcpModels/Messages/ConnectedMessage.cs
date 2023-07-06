using System;

namespace MatchTcpModels.Messages
{
    [Serializable]
    public class ConnectedMessage : Message
    {
        public string SessionToken;

        public ConnectedMessage()
        {
            Type = MessageType.Connected;
        }
    }
}
