using System;

namespace MatchTcpModels.Commands
{
    [Serializable]
    public class AuthenticateMatchUserSecretCommand : Command
    {
        public string UserSecret;
        /// <summary>Signed join token (JWT). Game servers that predate it ignore the field.</summary>
        public string JoinToken;

        public AuthenticateMatchUserSecretCommand() : base(CommandType.AuthenticateMatchUserSecret)
        {
        }
    }
}
