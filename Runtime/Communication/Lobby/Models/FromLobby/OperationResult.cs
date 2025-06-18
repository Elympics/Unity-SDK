using System;
using MessagePack;

#nullable enable

namespace Elympics.Lobby.Models
{
    [MessagePackObject]
    public record OperationResult(
        [property: Key(0)] Guid OperationId,
        [property: Key(1)] bool Success,
        [property: Key(2)] ErrorBlame? Blame,
        [property: Key(3)] ErrorKind? Kind,
        [property: Key(4)] string? Details) : IFromLobby
    {
        internal OperationResult(Guid operationId) : this(operationId, true, default, ErrorKind.Unspecified, null)
        { }

        internal OperationResult(Guid operationId, ErrorBlame blame, ErrorKind kind, string? details) : this(operationId, false, blame, kind, details)
        { }

        public string GetDescritpion() => $"{nameof(OperationId)}: {OperationId} {nameof(Success)}: {Success} {nameof(Blame)}: {Blame} {nameof(Kind)}: {Kind} {nameof(Details)}: {Details}";
    }
}
