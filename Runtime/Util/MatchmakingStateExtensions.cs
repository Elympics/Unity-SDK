using Elympics.Rooms.Models;

#nullable enable

namespace Elympics
{
    internal static class MatchmakingStateExtensions
    {
        public static bool IsInsideMatchmaking(this MatchmakingState? state) =>
            state is not (MatchmakingState.Unlocked or MatchmakingState.Playing or null);

        public static bool IsInsideMatchmakingOrMatch(this MatchmakingState? state) =>
            state is not (MatchmakingState.Unlocked or null);
        public static bool IsMatchMakingStateValidToCancel(this MatchmakingState state) =>
            state is MatchmakingState.Matchmaking or MatchmakingState.RequestingMatchmaking or MatchmakingState.CancellingMatchmaking;
    }
}
