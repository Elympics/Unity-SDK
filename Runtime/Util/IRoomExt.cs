namespace Elympics
{
    internal static class IRoomExt
    {
        public static bool IsMatchRoom(this IRoom room) => room.State.MatchmakingData is not null;
    }
}
