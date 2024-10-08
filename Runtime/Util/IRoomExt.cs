using Elympics.Rooms.Models;
namespace Elympics
{
    internal static class IRoomExt
    {
        public static bool IsMatchRoom(this IRoom room) => room.State.MatchmakingData is not null;

        public static bool IsEligibleToPlayMatch(this IRoom room) => room.State.MatchmakingData?.MatchmakingState == MatchmakingState.Playing;
    }
}
