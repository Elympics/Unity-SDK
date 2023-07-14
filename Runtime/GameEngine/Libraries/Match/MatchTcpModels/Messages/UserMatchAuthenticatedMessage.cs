using System;

namespace MatchTcpModels.Messages
{
    [Serializable]
    public class UserMatchAuthenticatedMessage : Message
    {
        public string UserId;
        public bool AuthenticatedSuccessfully;

        public UserMatchAuthenticatedMessage()
        {
            Type = MessageType.UserMatchAuthenticatedMessage;
        }
    }
}
