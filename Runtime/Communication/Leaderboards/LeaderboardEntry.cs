using System;

namespace Elympics
{
    public class LeaderboardEntry
    {
        public string UserId { get; }
        public string Nickname { get; }
        public int Position { get; }
        public float Score { get; }
        public DateTimeOffset? ScoredAt { get; }
        public string MatchId { get; }
        public Guid TournamentId { get; }

        [Obsolete("Use PlayPadCommunicator.LeaderboardCommunicator from PlayPad SDK instead.")]
        internal LeaderboardEntry(LeaderboardResponseEntry entry)
        {
            UserId = entry.userId;
            Position = entry.position;
            Score = entry.points;
            MatchId = entry.matchId;
            TournamentId = entry.tournamentId;
            Nickname = entry.nickname;

            if (DateTimeOffset.TryParse(entry.endedAt, out var endedAt))
                ScoredAt = endedAt;
        }
    }
}
