using System;
using Elympics.Communication.Rooms.Models;
using MessagePack;

#nullable enable

namespace Elympics.Lobby.Models
{
    [MessagePackObject]
    public record OperationResult(
        [property: Key(0)] Guid OperationId,
        [property: Key(1)] bool Success,
        [property: Key(2)] ErrorBlameDto? Blame,
        [property: Key(3)] ErrorKindDto? Kind,
        [property: Key(4)] string? Details) : IFromLobby
    {
        internal OperationResult(Guid operationId) : this(operationId, true, default, ErrorKindDto.Unspecified, null)
        { }

        internal OperationResult(Guid operationId, ErrorBlameDto blame, ErrorKindDto kind, string? details) : this(operationId, false, blame, kind, details)
        { }

        public string GetDescritpion() => $"{nameof(OperationId)}: {OperationId} {nameof(Success)}: {Success} {nameof(Blame)}: {Blame} {nameof(Kind)}: {Kind} {nameof(Details)}: {Details}";
    }
}
