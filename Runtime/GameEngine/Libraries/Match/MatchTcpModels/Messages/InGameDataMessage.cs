using System;

namespace MatchTcpModels.Messages
{
    [Serializable]
    public class InGameDataMessage : Message
    {
        public string Data;

        public InGameDataMessage()
        {
            Type = MessageType.InGameData;
        }
    }
}
