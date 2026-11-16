using System;

namespace MatchTcpModels.Commands
{
    [Serializable]
    public class AuthenticateAsSpectatorCommand : Command
    {
        /// <summary>Signed join token (JWT) with the spectator role. Game servers that predate it ignore the field.</summary>
        public string JoinToken;

        public AuthenticateAsSpectatorCommand() : base(CommandType.AuthenticateAsSpectator)
        {
        }
    }
}
