using System;

namespace Elympics.Rooms.Models
{
    public enum MatchmakingState
    {
        Unlocked = 0,
        RequestingMatchmaking = 1,
        Matchmaking = 2,
        CancellingMatchmaking = 3,
        [Obsolete("No longer used")] Matched = 4,
        Playing = 5,
    }
}
