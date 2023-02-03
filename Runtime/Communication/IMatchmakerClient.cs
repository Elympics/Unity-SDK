using System;
using System.Collections.Generic;
using System.Threading;

namespace Elympics
{
	public interface IMatchmakerClient
	{
		event Action<(string GameId, string GameVersion)>                 LookingForUnfinishedMatchStarted;
		event Action<(string GameId, string GameVersion, string MatchId)> LookingForUnfinishedMatchFinished;
		event Action<(string GameId, string GameVersion, string Error)>   LookingForUnfinishedMatchError;
		event Action<(string GameId, string GameVersion)>                 LookingForUnfinishedMatchCancelled;

		event Action<(string GameId, string GameVersion)>                 WaitingForMatchStarted;
		event Action<(string GameId, string GameVersion, string MatchId)> WaitingForMatchFinished;
		event Action<(string GameId, string GameVersion)>                 WaitingForMatchRetried;
		event Action<(string GameId, string GameVersion, string Error)>   WaitingForMatchError;
		event Action<(string GameId, string GameVersion)>                 WaitingForMatchCancelled;

		event Action<string>                         WaitingForMatchStateInitializingStartedWithMatchId;
		event Action<string>                         WaitingForMatchStateInitializingFinishedWithMatchId;
		event Action<string>                         WaitingForMatchStateInitializingRetriedWithMatchId;
		event Action<(string MatchId, string Error)> WaitingForMatchStateInitializingError;
		event Action<string>                         WaitingForMatchStateInitializingCancelledWithMatchId;

		event Action<string>                                                                                             WaitingForMatchStateRunningStartedWithMatchId;
		event Action<(string MatchId, string TcpUdpServerAddress, string WebServerAddress, List<string> MatchedPlayers)> WaitingForMatchStateRunningFinished;
		event Action<string>                                                                                             WaitingForMatchStateRunningRetriedWithMatchId;
		event Action<(string MatchId, string Error)>                                                                     WaitingForMatchStateRunningError;
		event Action<string>                                                                                             WaitingForMatchStateRunningCancelledWithMatchId;


		event Action                                                                                                                        MatchmakingStarted;
		event Action<(string MatchId, string TcpUdpServerAddress, string WebServerAddress, string UserSecret, List<string> MatchedPlayers)> MatchmakingFinished;
		event Action<string>                                                                                                                MatchmakingError;
		event Action                                                                                                                        MatchmakingCancelled;

		JoinedMatchData MatchData { get; }

		void JoinMatchmakerAsync(string gameId, string gameVersion, bool tryReconnect = false, float[] matchmakerData = null, byte[] gameEngineData = null, string queueName = null, CancellationToken ct = default, string regionName = null);
	}
}
