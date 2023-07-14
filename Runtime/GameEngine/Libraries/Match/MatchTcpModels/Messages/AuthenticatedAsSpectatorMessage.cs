using System;

namespace MatchTcpModels.Messages
{
    [Serializable]
    public class AuthenticatedAsSpectatorMessage : Message
    {
        public bool AuthenticatedSuccessfully;

        public AuthenticatedAsSpectatorMessage()
        {
            Type = MessageType.AuthenticateAsSpectator;
        }
    }
}
