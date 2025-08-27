using System;
using System.Collections.Generic;
using Elympics.Communication.Rooms.PublicModels;
using Elympics.Rooms.Models;
using JetBrains.Annotations;

#nullable enable

namespace Elympics
{
    /// <param name="RoomIds">A list with <see cref="IRoom.RoomId"/> of each room that was added, removed or had its state updated.</param>
    [PublicAPI] public record RoomListUpdatedArgs(IReadOnlyList<Guid> RoomIds);
    /// <param name="RoomId"><see cref="IRoom.RoomId"/> of the new value of the <see cref="IRoomsManager.CurrentRoom"/>.</param>
    [PublicAPI] public record JoinedRoomUpdatedArgs(Guid RoomId);

    /// <param name="User">Information about the user who just joined the <see cref="IRoomsManager.CurrentRoom"/>.</param>
    [PublicAPI] public record UserJoinedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId, UserInfo User);
    /// <param name="User">Information about the user who just left the <see cref="IRoomsManager.CurrentRoom"/>.</param>
    [PublicAPI] public record UserLeftArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId, UserInfo User);
    /// <param name="UserCount">New number of users in the <see cref="IRoomsManager.CurrentRoom"/>.</param>
    [PublicAPI] public record UserCountChangedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId, uint UserCount);
    /// <param name="UserId">ID of the new host.</param>
    [PublicAPI] public record HostChangedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId, Guid UserId);
    /// <param name="UserId">ID of the user who changed their readiness.</param>
    /// <param name="IsReady">True if the user became ready, false if the user became unready.</param>
    [PublicAPI] public record UserReadinessChangedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId, Guid UserId, bool IsReady);
    /// <param name="UserId">ID of the user who changed their team.</param>
    /// <param name="TeamIndex">
    /// Index of the team joined by the user.
    /// Currently null value is only used by some experimental features,
    /// so it is safe to assume in your code that this value will never be null,
    /// unless you are using related experimental features.
    /// </param>
    [PublicAPI] public record UserChangedTeamArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId, Guid UserId, uint? TeamIndex);
    /// <param name="Key">Custom room data key that was modified.</param>
    /// <param name="Value">New value associated with the <paramref name="Key"/> or null if the <paramref name="Key"/> was removed.</param>
    [PublicAPI] public record CustomRoomDataChangedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId, string Key, string? Value);

    /// <param name="IsPrivate">The new value of <see cref="RoomState.IsPrivate"/>.</param>
    [PublicAPI] public record RoomPublicnessChangedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId, bool IsPrivate);

    [PublicAPI] public record RoomBetAmountChangedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId, RoomBetAmount? NewBetValue);

    /// <param name="RoomName">The new value of <see cref="RoomState.RoomName"/>.</param>
    [PublicAPI] public record RoomNameChangedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId, string RoomName);

    /// <param name="RoomId"><see cref="IRoom.RoomId"/> of the room that was joined.</param>
    /// <seealso cref="IRoomsManager.CurrentRoom"/>
    [PublicAPI] public record JoinedRoomArgs(Guid RoomId);
    /// <param name="RoomId"><see cref="IRoom.RoomId"/> of the room that that user has left.</param>
    /// <param name="Reason">The reason why the user has left the room.</param>
    /// <seealso cref="IRoomsManager.CurrentRoom"/>
    [PublicAPI] public record LeftRoomArgs(Guid RoomId, LeavingReason Reason);

    [PublicAPI] public record MatchmakingStartedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId);
    [PublicAPI] public record MatchmakingEndedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId);
    [PublicAPI] public record MatchmakingDataChangedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId);
    /// <param name="MatchId">ID of the match.</param>
    /// <param name="QueueName">Name of the matchmaking queue</param>
    /// <param name="MatchData">Detailed data about the match, including its current state.</param>
    [PublicAPI] public record MatchDataReceivedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId, Guid MatchId, string QueueName, MatchData MatchData);
    /// <param name="Key">Custom matchmaking data key that was modified.</param>
    /// <param name="Value">New value associated with the <paramref name="Key"/> or null if the <paramref name="Key"/> was removed.</param>
    [PublicAPI] public record CustomMatchmakingDataChangedArgs([property: Obsolete("Use IRoomsManager.CurrentRoom.RoomId instead.")] Guid RoomId, string Key, string? Value);
}
