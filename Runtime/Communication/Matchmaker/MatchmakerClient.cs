using System;
using System.Linq;
using System.Threading;
using Elympics.Models;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using Elympics.Models.Matchmaking.LongPolling;
using MatchmakingRoutes = Elympics.Models.Matchmaking.Routes;

namespace Elympics
{
    internal abstract class MatchmakerClient : IMatchmakerEvents
    {
        private readonly UriBuilder _uriBuilder;
        private readonly string _baseUrlOriginalPath;

        private string GetUnfinishedMatchesUrl(Guid gameId, string gameVersion)
        {
            _uriBuilder.Path = string.Join("/", _baseUrlOriginalPath, MatchmakingRoutes.Base, MatchmakingRoutes.Unfinished, gameId.ToString(), gameVersion);
            return _uriBuilder.Uri.ToString();
        }

        private string GetUnfinishedMatchDetailsUrl(string matchId)
        {
            _uriBuilder.Path = string.Join("/", _baseUrlOriginalPath, MatchmakingRoutes.Base, MatchmakingRoutes.UnfinishedMatchDetails, matchId);
            return _uriBuilder.Uri.ToString();
        }

        internal void JoinMatchmakerAsync(JoinMatchmakerData joinMatchmakerData, AuthData authData, CancellationToken ct = default)
        {
            var modifiedData = new JoinMatchmakerData
            {
                GameId = ApplicationParameters.Parameters.GameId.GetValue(joinMatchmakerData.GameId),
                GameVersion = ApplicationParameters.Parameters.VersionName.GetValue(joinMatchmakerData.GameVersion),
                MatchmakerData = joinMatchmakerData.MatchmakerData,
                GameEngineData = joinMatchmakerData.GameEngineData,
                QueueName = ApplicationParameters.Parameters.Queue.GetValue(joinMatchmakerData.QueueName),
                RegionName = ApplicationParameters.Parameters.Region.GetValue(joinMatchmakerData.RegionName),
            };
            JoinMatchmakerInternal(modifiedData, authData, ct);
        }

        protected abstract void JoinMatchmakerInternal(JoinMatchmakerData joinMatchmakerData, AuthData authData, CancellationToken ct = default);

        public void CheckForAnyUnfinishedMatch(Guid gameId, string gameVersion, AuthData authData, Action<bool> onCompleted, Action<Exception> onError, CancellationToken ct = default)
        {
            var unfinishedMatchesUrl = GetUnfinishedMatchesUrl(gameId, gameVersion);
            ElympicsWebClient.SendGetRequest<UnfinishedMatchesResponse>(unfinishedMatchesUrl, null, authData.BearerAuthorization, OnResponse, ct);

            void OnResponse(Result<UnfinishedMatchesResponse, Exception> result)
            {
                if (result.IsSuccess)
                    onCompleted(result.Value.Matches?.Any() is true);
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

                matchId = result.Value?.Matches?.FirstOrDefault()?.MatchId;
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

        private static bool TryGetErrorMessage(Result<ApiResponse, Exception> result, out string errorMessage)
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

        internal void EmitMatchmakingStarted(Guid gameId, string gameVersion) =>
            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingStarted?.Invoke());

        internal void EmitMatchmakingSucceeded(MatchmakingFinishedData data) =>
            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingSucceeded?.Invoke(data));

        internal void EmitMatchmakingMatchFound(Guid matchId, Guid gameId, string gameVersion) =>
            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingMatchFound?.Invoke(matchId));

        internal void EmitMatchmakingFailed(string error, Guid matchId, Guid gameId, string gameVersion) =>
            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingFailed?.Invoke((error, matchId)));

        internal void EmitMatchmakingWarning(string warning, Guid matchId) =>
            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingWarning?.Invoke((warning, matchId)));

        internal void EmitMatchmakingCancelled(Guid matchId, Guid gameId, string gameVersion) =>
            AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingCancelledGuid?.Invoke(matchId));

        #endregion Matchmaking event emitters

        #region Matchmaking events

        public event Action MatchmakingStarted;
        public event Action<MatchmakingFinishedData> MatchmakingSucceeded;
        public event Action<Guid> MatchmakingMatchFound;
        public event Action<(string Error, Guid MatchId)> MatchmakingFailed;
        public event Action<(string Warning, Guid MatchId)> MatchmakingWarning;
        public event Action<Guid> MatchmakingCancelledGuid;

        #endregion Matchmaking events
    }
}
