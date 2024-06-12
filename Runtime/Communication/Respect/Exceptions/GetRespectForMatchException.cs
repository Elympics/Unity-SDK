using System;
using JetBrains.Annotations;

namespace Elympics
{
    public class GetRespectForMatchException : Exception
    {
        [PublicAPI]
        public readonly Guid MatchId;

        public GetRespectForMatchException(Guid matchId) : base($"Couldn't retrieve player's respect gained from match {matchId}") => MatchId = matchId;
    }
}
