using System;

namespace MatchTcpModels.Commands
{
    [Serializable]
    public class AuthenticateMatchUserSecretCommand : Command
    {
        public string UserSecret;

        public AuthenticateMatchUserSecretCommand() : base(CommandType.AuthenticateMatchUserSecret)
        {
        }
    }
}
