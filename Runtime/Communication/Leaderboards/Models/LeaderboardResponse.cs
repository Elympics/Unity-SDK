using System;

namespace Elympics
{
    internal class LeaderboardResponse : PaginatedResponseModel<LeaderboardResponseEntry> { }

    [Serializable]
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
