using System;

namespace Elympics
{
    public class LeaderboardEntry
    {
        /// <summary>
        /// Raw user ID.
        /// It is recommended to pair it with nicknames using your own external backend.
        /// </summary>
        public string UserId { get; }
        public int Position { get; }
        public float Score { get; }
        public DateTimeOffset ScoredAt { get; }

        internal LeaderboardEntry(LeaderboardResponseEntry entry)
        {
            UserId = entry.userId;
            Position = entry.position;
            Score = entry.points;
            ScoredAt = DateTimeOffset.Parse(entry.endedAt);
        }
    }
}
