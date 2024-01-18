using System;
using System.Collections.Generic;
using Elympics.Models.Matchmaking;
using Elympics.Models.Matchmaking.LongPolling;

// ReSharper disable EventNeverInvoked.Global
// ReSharper disable EventNeverSubscribedTo.Global

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

        #region Deprecated matchmaking events

        [Obsolete("Use " + nameof(ElympicsLobbyClient) + "." + nameof(ElympicsLobbyClient.RejoinLastOnlineMatch) + " instead")]
        event Action<(string GameId, string GameVersion)> LookingForUnfinishedMatchStarted;

        [Obsolete("Use " + nameof(ElympicsLobbyClient) + "." + nameof(ElympicsLobbyClient.RejoinLastOnlineMatch) + " instead")]
        event Action<(string GameId, string GameVersion, string MatchId)> LookingForUnfinishedMatchFinished;

        [Obsolete("Use " + nameof(ElympicsLobbyClient) + "." + nameof(ElympicsLobbyClient.RejoinLastOnlineMatch) + " instead")]
        event Action<(string GameId, string GameVersion, string Error)> LookingForUnfinishedMatchError;

        [Obsolete("Use " + nameof(ElympicsLobbyClient) + "." + nameof(ElympicsLobbyClient.RejoinLastOnlineMatch) + " instead")]
        event Action<(string GameId, string GameVersion)> LookingForUnfinishedMatchCancelled;


        [Obsolete("Use " + nameof(MatchmakingStarted) + " instead")]
        event Action<(string GameId, string GameVersion)> WaitingForMatchStarted;

        [Obsolete("Use " + nameof(MatchmakingMatchFound) + " instead")]
        event Action<(string GameId, string GameVersion, string MatchId)> WaitingForMatchFinished;

        [Obsolete("Not used anymore. Retries have been removed")]
        event Action<(string GameId, string GameVersion)> WaitingForMatchRetried;

        [Obsolete("Use " + nameof(MatchmakingFailed) + " instead")]
        event Action<(string GameId, string GameVersion, string Error)> WaitingForMatchError;

        [Obsolete("Use " + nameof(MatchmakingCancelledGuid) + " instead")]
        event Action<(string GameId, string GameVersion)> WaitingForMatchCancelled;


        [Obsolete("Not used anymore. Only " + nameof(GetMatchDesiredState.Running) + " match state is awaited")]
        event Action<string> WaitingForMatchStateInitializingStartedWithMatchId;

        [Obsolete("Not used anymore. Only " + nameof(GetMatchDesiredState.Running) + " match state is awaited")]
        event Action<string> WaitingForMatchStateInitializingFinishedWithMatchId;

        [Obsolete("Not used anymore. Only " + nameof(GetMatchDesiredState.Running) + " match state is awaited")]
        event Action<string> WaitingForMatchStateInitializingRetriedWithMatchId;

        [Obsolete("Not used anymore. Only " + nameof(GetMatchDesiredState.Running) + " match state is awaited")]
        event Action<(string MatchId, string Error)> WaitingForMatchStateInitializingError;

        [Obsolete("Not used anymore. Only " + nameof(GetMatchDesiredState.Running) + " match state is awaited")]
        event Action<string> WaitingForMatchStateInitializingCancelledWithMatchId;


        [Obsolete("Use " + nameof(MatchmakingMatchFound) + " instead")]
        event Action<string> WaitingForMatchStateRunningStartedWithMatchId;

        [Obsolete("Use " + nameof(MatchmakingSucceeded) + " instead")]
        event Action<(string MatchId, string TcpUdpServerAddress, string WebServerAddress, List<string> MatchedPlayers)> WaitingForMatchStateRunningFinished;

        [Obsolete("Not used anymore. Retries have been removed")]
        event Action<string> WaitingForMatchStateRunningRetriedWithMatchId;

        [Obsolete("Use " + nameof(MatchmakingFailed) + " instead")]
        event Action<(string MatchId, string Error)> WaitingForMatchStateRunningError;

        [Obsolete("Use " + nameof(MatchmakingCancelledGuid) + " instead")]
        event Action<string> WaitingForMatchStateRunningCancelledWithMatchId;


        [Obsolete("Use " + nameof(MatchmakingSucceeded) + " instead")]
        event Action<(string MatchId, string TcpUdpServerAddress, string WebServerAddress, string UserSecret, List<string> MatchedPlayers)> MatchmakingFinished;

        [Obsolete("Use " + nameof(MatchmakingFailed) + " instead")]
        event Action<string> MatchmakingError;

        [Obsolete("Use " + nameof(MatchmakingCancelledGuid) + " instead")]
        event Action MatchmakingCancelled;

        #endregion Deprecated matchmaking events
    }
}
