using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    public readonly struct TournamentInfo
    {
        public int LeaderboardCapacity { get; init; }
        public string Name { get; init; }
        public Guid OwnerId { get; init; }
        public TournamentState State { get; init; }
        public DateTimeOffset CreateDate { get; init; }
        public DateTimeOffset StartDate { get; init; }
        public DateTimeOffset EndDate { get; init; }
        public List<string> Participants { get; init; }


    }
}
