using System;
using Elympics.Lobby.Models;

#nullable enable

namespace Elympics
{
    public class LobbyOperationException : ElympicsException
    {
        public readonly ErrorBlame? Blame;
        public readonly ErrorKind? Kind;

        internal LobbyOperationException(OperationResultDto result) : base(result.Details ?? result.Kind.ToString())
        {
            Blame = result.Blame?.Map();
            Kind = result.Kind?.Map();
        }

        internal LobbyOperationException(string message) : base(message)
        { }
    }

    public class ConfirmationTimeoutException : LobbyOperationException
    {
        public readonly TimeSpan? ExpectedTimeSpan;

        internal ConfirmationTimeoutException(TimeSpan? expectedTimeSpan = null)
            : base("Confirmation from lobby has not been received in the expected time span") =>
            ExpectedTimeSpan = expectedTimeSpan;
    }
}
