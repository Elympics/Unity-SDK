using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elympics.Models;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using Elympics.Models.Matchmaking.LongPolling;
using MatchmakingRoutes = Elympics.Models.Matchmaking.Routes;

#pragma warning disable CS0067

namespace Elympics
{
    internal abstract class MatchmakerClient : IMatchmakerEvents
    {
        private readonly UriBuilder _uriBuilder;
        private readonly string _baseUrlOriginalPath;

        private string GetUnfinishedMatchesUrl(Guid gameId, string gameVersion)
        {
            _uriBuilder.Path = string.Join("/", _baseUrlOriginalPath, MatchmakingRoutes.Base, MatchmakingRoutes.UnfinishedMatches, gameId.ToString(), gameVersion);
            return _uriBuilder.Uri.ToString();
        }

        private string GetUnfinishedMatchDetailsUrl(string matchId)
        {
            _uriBuilder.Path = string.Join("/", _baseUrlOriginalPath, MatchmakingRoutes.Base, MatchmakingRoutes.UnfinishedMatchDetails, matchId);
            return _uriBuilder.Uri.ToString();
        }

        internal abstract void JoinMatchmakerAsync(JoinMatchmakerData joinMatchmakerData, AuthData authData, CancellationToken ct = default);

        public void CheckForAnyUnfinishedMatch(Guid gameId, string gameVersion, AuthData authData, Action<bool> onCompleted, Action<Exception> onError, CancellationToken ct = default)
        {
            var unfinishedMatchesUrl = GetUnfinishedMatchesUrl(gameId, gameVersion);
            ElympicsWebClient.SendGetRequest<UnfinishedMatchesResponse>(unfinishedMatchesUrl, null, authData.BearerAuthorization, OnResponse, ct);

            void OnResponse(Result<UnfinishedMatchesResponse, Exception> result)
            {
                if (result.IsSuccess)
                    onCompleted((result.Value.MatchIds?.Count ?? 0) > 0);
                else
                    onError(result.Error);
            }
        }

        public void RejoinLastMatchAsync(Guid gameId, string gameVersion, AuthData authData, CancellationToken ct = default)
        {
            var unfinishedMatchesUrl = GetUnfinishedMatchesUrl(gameId, gameVersion);
            string matchId = null;

            EmitMatchmakingStarted(gameId, gameVersion);

            if (ct.IsCancellationRequested)
            {
                EmitMatchmakingCancelled(Guid.Empty, gameId, gameVersion);
                return;
            }

            ElympicsWebClient.SendGetRequest<UnfinishedMatchesResponse>(unfinishedMatchesUrl, null, authData.BearerAuthorization, OnListResponse, ct);

            void OnListResponse(Result<UnfinishedMatchesResponse, Exception> result)
            {
                if (ct.IsCancellationRequested)
                {
                    EmitMatchmakingCancelled(Guid.Empty, gameId, gameVersion);
                    return;
                }
                if (result.IsFailure)
                {
                    EmitMatchmakingFailed($"Error fetching unfinished match list: {result.Error.Message}", Guid.Empty, gameId, gameVersion);
                    return;
                }

                matchId = result.Value?.MatchIds?.FirstOrDefault();
                if (string.IsNullOrEmpty(matchId))
                {
                    EmitMatchmakingFailed("No unfinished matches to rejoin", Guid.Empty, gameId, gameVersion);
                    return;
                }

                var unfinishedMatchDetailsUrl = GetUnfinishedMatchDetailsUrl(matchId);
                ElympicsWebClient.SendGetRequest<GetMatchModel.Response>(unfinishedMatchDetailsUrl, null, authData.BearerAuthorization, OnDetailsResponse, ct);
            }

            void OnDetailsResponse(Result<GetMatchModel.Response, Exception> result)
            {
                if (ct.IsCancellationRequested)
                {
                    EmitMatchmakingCancelled(Guid.Empty, gameId, gameVersion);
                    return;
                }
                if (TryGetErrorMessage(Result<ApiResponse, Exception>.Generalize(result), out var errorMessage))
                {
                    EmitMatchmakingFailed(errorMessage, new Guid(matchId), gameId, gameVersion);
                    return;
                }

                EmitMatchmakingSucceeded(new MatchmakingFinishedData(result.Value));
            }
        }

        protected static bool TryGetErrorMessage(Result<ApiResponse, Exception> result, out string errorMessage)
        {
            if (result.IsFailure)
            {
                errorMessage = result.Error.Message;
                return true;
            }

            if (result.Value is null)
            {
                errorMessage = "Received empty response";
                return true;
            }
            if (!string.IsNullOrEmpty(result.Value.ErrorMessage))
            {
                errorMessage = result.Value.ErrorMessage;
                return true;
            }

            errorMessage = null;
            return false;
        }

        protected MatchmakerClient(string baseUrl)
        {
            _uriBuilder = new UriBuilder(baseUrl);
            _baseUrlOriginalPath = _uriBuilder.Path.TrimEnd('/');
        }

        #region Matchmaking event emitters

        internal void EmitMatchmakingStarted(Guid gameId, string gameVersion)
        {
            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingStarted?.Invoke());

            AsyncEventsDispatcher.Instance.Enqueue(() => WaitingForMatchStarted?.Invoke((gameId.ToString(), gameVersion)));
        }

        internal void EmitMatchmakingSucceeded(MatchmakingFinishedData data)
        {
            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingSucceeded?.Invoke(data));

            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingFinished?.Invoke((data.MatchId.ToString(),
                data.TcpUdpServerAddress, data.WebServerAddress, data.UserSecret, data.MatchedPlayers.Select(x => x.ToString()).ToList())));
            AsyncEventsDispatcher.Instance.Enqueue(() => WaitingForMatchStateRunningFinished?.Invoke((data.MatchId.ToString(),
                data.TcpUdpServerAddress, data.WebServerAddress, data.MatchedPlayers.Select(x => x.ToString()).ToList())));
        }

        internal void EmitMatchmakingMatchFound(Guid matchId, Guid gameId, string gameVersion)
        {
            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingMatchFound?.Invoke(matchId));

            AsyncEventsDispatcher.Instance.Enqueue(() => WaitingForMatchFinished?.Invoke((gameId.ToString(), gameVersion, matchId.ToString())));
            AsyncEventsDispatcher.Instance.Enqueue(() => WaitingForMatchStateRunningStartedWithMatchId?.Invoke(matchId.ToString()));
        }

        internal void EmitMatchmakingFailed(string error, Guid matchId, Guid gameId, string gameVersion)
        {
            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingFailed?.Invoke((error, matchId)));

            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingError?.Invoke(error));
            if (matchId == Guid.Empty)
                AsyncEventsDispatcher.Instance.Enqueue(() => WaitingForMatchError?.Invoke((gameId.ToString(), gameVersion, error)));
            else
                AsyncEventsDispatcher.Instance.Enqueue(() => WaitingForMatchStateRunningError?.Invoke((matchId.ToString(), error)));
        }

        internal void EmitMatchmakingWarning(string warning, Guid matchId)
        {
            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingWarning?.Invoke((warning, matchId)));
        }

        internal void EmitMatchmakingCancelled(Guid matchId, Guid gameId, string gameVersion)
        {
            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingCancelledGuid?.Invoke(matchId));

            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingCancelled?.Invoke());
            if (matchId == Guid.Empty)
                AsyncEventsDispatcher.Instance.Enqueue(() => WaitingForMatchCancelled?.Invoke((gameId.ToString(), gameVersion)));
            else
                AsyncEventsDispatcher.Instance.Enqueue(() => WaitingForMatchStateRunningCancelledWithMatchId?.Invoke(matchId.ToString()));
        }

        #endregion Matchmaking event emitters

        #region Matchmaking events

        public event Action MatchmakingStarted;
        public event Action<MatchmakingFinishedData> MatchmakingSucceeded;
        public event Action<Guid> MatchmakingMatchFound;
        public event Action<(string Error, Guid MatchId)> MatchmakingFailed;
        public event Action<(string Warning, Guid MatchId)> MatchmakingWarning;
        public event Action<Guid> MatchmakingCancelledGuid;

        #endregion Matchmaking events

        #region Deprecated matchmaking events

        public event Action<(string GameId, string GameVersion)> LookingForUnfinishedMatchStarted;
        public event Action<(string GameId, string GameVersion, string MatchId)> LookingForUnfinishedMatchFinished;
        public event Action<(string GameId, string GameVersion, string Error)> LookingForUnfinishedMatchError;
        public event Action<(string GameId, string GameVersion)> LookingForUnfinishedMatchCancelled;

        public event Action<(string GameId, string GameVersion)> WaitingForMatchStarted;
        public event Action<(string GameId, string GameVersion, string MatchId)> WaitingForMatchFinished;
        public event Action<(string GameId, string GameVersion)> WaitingForMatchRetried;
        public event Action<(string GameId, string GameVersion, string Error)> WaitingForMatchError;
        public event Action<(string GameId, string GameVersion)> WaitingForMatchCancelled;

        public event Action<string> WaitingForMatchStateInitializingStartedWithMatchId;
        public event Action<string> WaitingForMatchStateInitializingFinishedWithMatchId;
        public event Action<string> WaitingForMatchStateInitializingRetriedWithMatchId;
        public event Action<(string MatchId, string Error)> WaitingForMatchStateInitializingError;
        public event Action<string> WaitingForMatchStateInitializingCancelledWithMatchId;

        public event Action<string> WaitingForMatchStateRunningStartedWithMatchId;
        public event Action<(string MatchId, string TcpUdpServerAddress, string WebServerAddress, List<string> MatchedPlayers)> WaitingForMatchStateRunningFinished;
        public event Action<string> WaitingForMatchStateRunningRetriedWithMatchId;
        public event Action<(string MatchId, string Error)> WaitingForMatchStateRunningError;
        public event Action<string> WaitingForMatchStateRunningCancelledWithMatchId;

        public event Action<(string MatchId, string TcpUdpServerAddress, string WebServerAddress, string UserSecret, List<string> MatchedPlayers)> MatchmakingFinished;
        public event Action<string> MatchmakingError;
        public event Action MatchmakingCancelled;

        #endregion Deprecated matchmaking events
    }
}
