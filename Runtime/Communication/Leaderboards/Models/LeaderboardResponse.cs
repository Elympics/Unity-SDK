using System;

namespace Elympics
{
    [Obsolete("Use PlayPadCommunicator.LeaderboardCommunicator from PlayPad SDK instead.")]
    internal class LeaderboardResponse : PaginatedResponseModel<LeaderboardResponseEntry> { }

    [Serializable, Obsolete("Use PlayPadCommunicator.LeaderboardCommunicator from PlayPad SDK instead.")]
    internal class LeaderboardResponseEntry
    {
        public string userId;
        public string matchId;
        public Guid tournamentId;
        public int position;
        public float points;
        public string endedAt;
        public string nickname;
    };
}
