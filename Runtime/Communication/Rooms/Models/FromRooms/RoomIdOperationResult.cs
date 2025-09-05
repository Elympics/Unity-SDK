using System;
using Elympics.Communication.Rooms.Models;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record RoomIdOperationResult(
        Guid OperationId,
        bool Success,
        ErrorBlameDto? Blame,
        ErrorKindDto? Kind,
        string? Details,
        [property: Key(5)] Guid RoomId) : OperationResult(OperationId, Success, Blame, Kind, Details)
    {
        internal RoomIdOperationResult(Guid operationId, Guid roomId) : this(operationId, true, default, ErrorKindDto.Unspecified, null, roomId)
        { }

        internal RoomIdOperationResult(Guid operationId, ErrorBlameDto blame, ErrorKindDto kind, string? details, Guid roomId) : this(operationId, false, blame, kind, details, roomId)
        { }
    }
}
