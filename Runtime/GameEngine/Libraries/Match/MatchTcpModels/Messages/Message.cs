using System;

namespace MatchTcpModels.Messages
{
    [Serializable]
    public class Message
    {
        public MessageType Type;
        public string ErrorMessage;

        public Message()
        {
        }
    }
}
