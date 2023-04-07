using System;
using System.Linq;
using System.Threading;
using Elympics.Models.Matchmaking;
using Elympics.Models.Matchmaking.LongPolling;

namespace Elympics
{
	internal class LongPollingMatchmakerClient : MatchmakerClient
	{
		internal LongPollingMatchmakerClient(IUserApiClient userApiClient) : base(userApiClient)
		{ }

		internal override void JoinMatchmakerAsync(JoinMatchmakerData joinMatchmakerData, CancellationToken ct = default)
		{
			var gameId = joinMatchmakerData.GameId;
			var gameVersion = joinMatchmakerData.GameVersion;

			void OnMatched(JoinMatchmakerAndWaitForMatchModel.Response response, Exception exception)
			{
				var matchId = response.MatchId != null ? new Guid(response.MatchId) : Guid.Empty;
				if (ct.IsCancellationRequested)
				{
					EmitMatchmakingCancelled(matchId, gameId, gameVersion);
					return;
				}

				if (exception != null)
				{
					EmitMatchmakingFailed(exception.Message, matchId, gameId, gameVersion);
					return;
				}

				if (IsOpponentNotFound(response))
				{
					EmitMatchmakingFailed(JoinMatchmakerAndWaitForMatchModel.ErrorCodes.OpponentNotFound, matchId, gameId, gameVersion);
					return;
				}

				EmitMatchmakingMatchFound(matchId, gameId, gameVersion);

				var getMatchRequest = new GetMatchModel.Request
				{
					MatchId = response.MatchId,
					DesiredState = GetMatchDesiredState.Initializing,
				};
				UserApiClient.GetMatchLongPolling(getMatchRequest, OnServerInitializing, ct);
			}

			void OnServerInitializing(GetMatchModel.Response response, Exception exception)
			{
				var matchId = response.MatchId != null ? new Guid(response.MatchId) : Guid.Empty;
				if (ct.IsCancellationRequested)
				{
					EmitMatchmakingCancelled(matchId, gameId, gameVersion);
					return;
				}

				if (TryGetErrorMessage(response, exception, out var errorMessage))
				{
					errorMessage += $" (waiting for state: {GetMatchDesiredState.Initializing}";
					EmitMatchmakingFailed(errorMessage, matchId, gameId, gameVersion);
					return;
				}

				var getMatchRequest = new GetMatchModel.Request
				{
					MatchId = response.MatchId,
					DesiredState = GetMatchDesiredState.Running,
				};
				UserApiClient.GetMatchLongPolling(getMatchRequest, OnServerRunning, ct);
			}

			void OnServerRunning(GetMatchModel.Response response, Exception exception)
			{
				var matchId = response.MatchId != null ? new Guid(response.MatchId) : Guid.Empty;
				if (ct.IsCancellationRequested)
				{
					EmitMatchmakingCancelled(matchId, gameId, gameVersion);
					return;
				}

				if (TryGetErrorMessage(response, exception, out var errorMessage))
				{
					errorMessage += $" (waiting for state: {GetMatchDesiredState.Running}";
					EmitMatchmakingFailed(errorMessage, matchId, gameId, gameVersion);
					return;
				}

				var matchmakerData = response.UserData?.MatchmakerData;
				var gameEngineData = Convert.FromBase64String(response.UserData?.GameEngineData ?? "");
				if (string.IsNullOrEmpty(response.UserData?.UserId)) // HACK: Unity initializes all scalars recursively with default values, so it is not certain if GE and MM data has been set to null by backend or omitted in the response
				{
					matchmakerData = joinMatchmakerData.MatchmakerData;
					gameEngineData = joinMatchmakerData.GameEngineData;
				}

				var matchData = new MatchmakingFinishedData(matchId, response.UserSecret,
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
			UserApiClient.JoinMatchmakerAndWaitForMatch(getPendingMatchRequest, OnMatched, ct);
		}

		private static bool TryGetErrorMessage(GetMatchModel.Response response, Exception exception, out string errorMessage)
		{
			if (exception != null)
			{
				errorMessage = exception.Message;
				return true;
			}

			if (IsMatchNotInDesiredState(response))
			{
				errorMessage = response?.ErrorMessage ?? "null response";
				return true;
			}

			errorMessage = null;
			return false;
		}

		private static bool IsOpponentNotFound(JoinMatchmakerAndWaitForMatchModel.Response response) =>
			response != null
			&& !response.IsSuccess
			&& response.ErrorMessage == JoinMatchmakerAndWaitForMatchModel.ErrorCodes.OpponentNotFound;

		private static bool IsMatchNotInDesiredState(GetMatchModel.Response response) =>
			response != null
			&& !response.IsSuccess
			&& response.ErrorMessage == GetMatchModel.ErrorCodes.NotInDesiredState;
	}
}
