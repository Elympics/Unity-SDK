using System;
using System.Collections.Generic;
using Elympics.Rooms.Models;
using JetBrains.Annotations;

#nullable enable

namespace Elympics
{
    [PublicAPI]
    public record RoomListUpdatedArgs(IReadOnlyList<Guid> RoomIds);
    [PublicAPI]
    public record JoinedRoomUpdatedArgs(Guid RoomId);

    [PublicAPI]
    public record UserJoinedArgs(Guid RoomId, UserInfo User);
    [PublicAPI]
    public record UserLeftArgs(Guid RoomId, UserInfo User);
    [PublicAPI]
    public record UserCountChangedArgs(Guid RoomId, uint UserCount);
    [PublicAPI]
    public record HostChangedArgs(Guid RoomId, Guid UserId);
    [PublicAPI]
    public record UserReadinessChangedArgs(Guid RoomId, Guid UserId, bool IsReady);
    [PublicAPI]
    public record UserChangedTeamArgs(Guid RoomId, Guid UserId, uint? TeamIndex);
    [PublicAPI]
    public record CustomRoomDataChangedArgs(Guid RoomId, string Key, string? Value);

    [PublicAPI]
    public record RoomPublicnessChangedArgs(Guid RoomId, bool IsPrivate);

    [PublicAPI]
    public record RoomNameChangedArgs(Guid RoomId, string RoomName);

    [PublicAPI]
    public record JoinedRoomArgs(Guid RoomId);
    [PublicAPI]
    public record LeftRoomArgs(Guid RoomId, LeavingReason Reason);

    [PublicAPI]
    public record MatchmakingStartedArgs(Guid RoomId);
    [PublicAPI]
    public record MatchmakingEndedArgs(Guid RoomId);
    [PublicAPI]
    public record MatchmakingDataChangedArgs(Guid RoomId);
    [PublicAPI]
    public record MatchDataReceivedArgs(Guid RoomId, Guid MatchId, string QueueName, MatchData MatchData);
    [PublicAPI]
    public record CustomMatchmakingDataChangedArgs(Guid RoomId, string Key, string? Value);
}
