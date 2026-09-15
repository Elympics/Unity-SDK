using System;
using Elympics.Models.Matchmaking;

namespace Elympics
{
    public interface IMatchmakerEvents
    {
        event Action MatchmakingStarted;
        event Action<MatchmakingFinishedData> MatchmakingSucceeded;
        event Action<Guid> MatchmakingMatchFound;
        event Action<(string Error, Guid MatchId)> MatchmakingFailed;
        event Action<(string Warning, Guid MatchId)> MatchmakingWarning;
        event Action<Guid> MatchmakingCancelledGuid;
    }
}
