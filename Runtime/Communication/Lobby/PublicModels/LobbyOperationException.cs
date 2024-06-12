using Elympics.Lobby.Models;

#nullable enable

namespace Elympics
{
    public class LobbyOperationException : ElympicsException
    {
        public readonly ErrorBlame? Blame;
        public readonly ErrorKind? Kind;

        internal LobbyOperationException(OperationResult result) : base(result.Details ?? result.Kind.ToString())
        {
            Blame = result.Blame;
            Kind = result.Kind;
        }

        internal LobbyOperationException(string message) : base(message)
        { }
    }
}
