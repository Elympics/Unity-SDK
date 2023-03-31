using System;
using Elympics.Models.Matchmaking;

namespace Elympics
{
	public interface IMatchmakerEvents
	{
		event Action MatchmakingStarted;
		event Action<MatchmakingFinishedData> MatchmakingFinished;
		event Action<Guid> MatchmakingMatchFound;
		event Action<(string Error, Guid MatchId)> MatchmakingError;
		event Action<(string Warning, Guid MatchId)> MatchmakingWarning;
		event Action<Guid> MatchmakingCancelled;
	}
}
