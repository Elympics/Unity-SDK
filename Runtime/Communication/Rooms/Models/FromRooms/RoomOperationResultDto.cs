using System;
using Elympics.Communication.Rooms.Models;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record RoomOperationResultDto(
        Guid OperationId,
        bool Success,
        ErrorBlameDto? Blame,
        ErrorKindDto? Kind,
        string? Details,
        [property: Key(5)] Guid RoomId) : OperationResultDto(OperationId, Success, Blame, Kind, Details)
    {
        internal RoomOperationResultDto(Guid operationId, Guid roomId) : this(operationId, true, default, ErrorKindDto.Unspecified, null, roomId)
        { }

        internal RoomOperationResultDto(Guid operationId, ErrorBlameDto blame, ErrorKindDto kind, string? details, Guid roomId) : this(operationId, false, blame, kind, details, roomId)
        { }
    }
}
