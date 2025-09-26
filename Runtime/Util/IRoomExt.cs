using Elympics.Rooms.Models;

#nullable enable

namespace Elympics
{
    internal static class IRoomExt
    {
        public static bool IsMatchRoom(this IRoom room) => room.State.MatchmakingData is not null;

        public static bool IsEligibleToPlayMatch(this IRoom room) => room.State.MatchmakingData?.MatchmakingState == MatchmakingState.Playing;

        public static bool IsDuringMatchmaking(this IRoom room) => room.State.MatchmakingData?.MatchmakingState is MatchmakingState.Matchmaking or MatchmakingState.RequestingMatchmaking;

        public static bool CanMatchmakingBeCancelled(this IRoom room) =>
            room.State.MatchmakingData?.MatchmakingState is MatchmakingState.RequestingMatchmaking or MatchmakingState.Matchmaking or MatchmakingState.CancellingMatchmaking;
    }
}
