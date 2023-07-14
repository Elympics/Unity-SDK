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
    internal class LongPollingMatchmakerClient : MatchmakerClient
    {
        private readonly string _getPendingMatchLongPollingUrl;
        private readonly string _getMatchLongPollingUrl;

        internal LongPollingMatchmakerClient(string baseUrl) : base(baseUrl)
        {
            var uriBuilder = new UriBuilder(baseUrl);
            var oldPath = uriBuilder.Path.TrimEnd('/');
            uriBuilder.Path = string.Join("/", oldPath, MatchmakingRoutes.Base, MatchmakingRoutes.GetPendingMatchLongPolling);
            _getPendingMatchLongPollingUrl = uriBuilder.Uri.ToString();
            uriBuilder.Path = string.Join("/", oldPath, MatchmakingRoutes.Base, MatchmakingRoutes.GetMatchLongPolling);
            _getMatchLongPollingUrl = uriBuilder.Uri.ToString();
        }

        internal override void JoinMatchmakerAsync(JoinMatchmakerData joinMatchmakerData, AuthData authData, CancellationToken ct = default)
        {
            var gameId = joinMatchmakerData.GameId;
            var gameVersion = joinMatchmakerData.GameVersion;
            var matchId = (string)null;
            var matchGuid = Guid.Empty;

            void OnMatched(Result<JoinMatchmakerAndWaitForMatchModel.Response, Exception> result)
            {
                if (ct.IsCancellationRequested)
                {
                    EmitMatchmakingCancelled(matchGuid, gameId, gameVersion);
                    return;
                }
                if (TryGetErrorMessage(result, out var errorMessage))
                {
                    EmitMatchmakingFailed(errorMessage, matchGuid, gameId, gameVersion);
                    return;
                }

                matchId = result.Value.MatchId;
                matchGuid = new Guid(matchId);
                EmitMatchmakingMatchFound(matchGuid, gameId, gameVersion);

                var getMatchRequest = new GetMatchModel.Request
                {
                    MatchId = matchId,
                    DesiredState = GetMatchDesiredState.Initializing,
                };
                ElympicsWebClient.SendPostRequest<GetMatchModel.Response>(_getMatchLongPollingUrl, getMatchRequest,
                    authData.BearerAuthorization, OnServerInitializing, ct);
            }

            void OnServerInitializing(Result<GetMatchModel.Response, Exception> result)
            {
                if (ct.IsCancellationRequested)
                {
                    EmitMatchmakingCancelled(matchGuid, gameId, gameVersion);
                    return;
                }
                if (TryGetErrorMessage(Result<ApiResponse, Exception>.Generalize(result), out var errorMessage))
                {
                    errorMessage += $" (waiting for state: {GetMatchDesiredState.Initializing}";
                    EmitMatchmakingFailed(errorMessage, matchGuid, gameId, gameVersion);
                    return;
                }

                var getMatchRequest = new GetMatchModel.Request
                {
                    MatchId = matchId,
                    DesiredState = GetMatchDesiredState.Running,
                };
                ElympicsWebClient.SendPostRequest<GetMatchModel.Response>(_getMatchLongPollingUrl, getMatchRequest,
                    authData.BearerAuthorization, OnServerRunning, ct);
            }

            void OnServerRunning(Result<GetMatchModel.Response, Exception> result)
            {
                if (ct.IsCancellationRequested)
                {
                    EmitMatchmakingCancelled(matchGuid, gameId, gameVersion);
                    return;
                }
                if (TryGetErrorMessage(Result<ApiResponse, Exception>.Generalize(result), out var errorMessage))
                {
                    errorMessage += $" (waiting for state: {GetMatchDesiredState.Running}";
                    EmitMatchmakingFailed(errorMessage, matchGuid, gameId, gameVersion);
                    return;
                }

                var response = result.Value;
                var matchmakerData = response.UserData?.MatchmakerData;
                var gameEngineData = Convert.FromBase64String(response.UserData?.GameEngineData ?? "");
                if (string.IsNullOrEmpty(response.UserData?.UserId))  // HACK: Unity initializes all primitives recursively with default values, so it is not certain if GE and MM data has been set to null by backend or omitted in the response
                {
                    matchmakerData = joinMatchmakerData.MatchmakerData;
                    gameEngineData = joinMatchmakerData.GameEngineData;
                }

                var matchData = new MatchmakingFinishedData(matchGuid, response.UserSecret,
                    joinMatchmakerData.QueueName, joinMatchmakerData.RegionName, gameEngineData, matchmakerData,
                    response.TcpUdpServerAddress, response.WebServerAddress, response.MatchedPlayersId.Select(x => new Guid(x)).ToArray());
                EmitMatchmakingSucceeded(matchData);
            }

            EmitMatchmakingStarted(gameId, gameVersion);

            if (ct.IsCancellationRequested)
            {
                EmitMatchmakingCancelled(Guid.Empty, gameId, gameVersion);
                return;
            }

            var getPendingMatchRequest = new JoinMatchmakerAndWaitForMatchModel.Request(joinMatchmakerData);
            ElympicsWebClient.SendPostRequest<JoinMatchmakerAndWaitForMatchModel.Response>(
                _getPendingMatchLongPollingUrl, getPendingMatchRequest, authData.BearerAuthorization, OnMatched, ct);
        }

        private static bool TryGetErrorMessage(Result<JoinMatchmakerAndWaitForMatchModel.Response, Exception> result, out string errorMessage)
        {
            if (TryGetErrorMessage(Result<ApiResponse, Exception>.Generalize(result), out errorMessage))
                return true;

            if (string.IsNullOrEmpty(result.Value.MatchId))
            {
                errorMessage = "Match ID missing from response";
                return true;
            }

            errorMessage = null;
            return false;
        }
    }
}
