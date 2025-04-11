using Elympics.Rooms.Models;

namespace Elympics
{
    internal static class MatchmakingStateExtensions
    {
        public static bool IsInsideMatchmaking(this MatchmakingState? state) =>
            state is not (MatchmakingState.Unlocked or MatchmakingState.Playing or null);

        public static bool IsInsideMatchmakingOrMatch(this MatchmakingState? state) =>
            state is not (MatchmakingState.Unlocked or null);
    }
}
