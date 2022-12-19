using System;
using System.Collections.Generic;
using System.Threading;
using LobbyPublicApiClients.Abstraction.User;
using LobbyPublicApiModels.User.Matchmaking;

namespace Elympics
{
	public class RemoteMatchmakerClient : IMatchmakerClient
	{
		private readonly IUserApiClient _userApiClient;

		public event Action<(string GameId, string GameVersion)>                 LookingForUnfinishedMatchStarted;
		public event Action<(string GameId, string GameVersion, string MatchId)> LookingForUnfinishedMatchFinished;
		public event Action<(string GameId, string GameVersion, string Error)>   LookingForUnfinishedMatchError;
		public event Action<(string GameId, string GameVersion)>                 LookingForUnfinishedMatchCancelled;

		public event Action<(string GameId, string GameVersion)>                 WaitingForMatchStarted;
		public event Action<(string GameId, string GameVersion, string MatchId)> WaitingForMatchFinished;
		public event Action<(string GameId, string GameVersion)>                 WaitingForMatchRetried;
		public event Action<(string GameId, string GameVersion, string Error)>   WaitingForMatchError;
		public event Action<(string GameId, string GameVersion)>                 WaitingForMatchCancelled;

		public event Action<string>                         WaitingForMatchStateInitializingStartedWithMatchId;
		public event Action<string>                         WaitingForMatchStateInitializingFinishedWithMatchId;
		public event Action<string>                         WaitingForMatchStateInitializingRetriedWithMatchId;
		public event Action<(string MatchId, string Error)> WaitingForMatchStateInitializingError;
		public event Action<string>                         WaitingForMatchStateInitializingCancelledWithMatchId;

		public event Action<string>                                                                                             WaitingForMatchStateRunningStartedWithMatchId;
		public event Action<(string MatchId, string TcpUdpServerAddress, string WebServerAddress, List<string> MatchedPlayers)> WaitingForMatchStateRunningFinished;
		public event Action<string>                                                                                             WaitingForMatchStateRunningRetriedWithMatchId;
		public event Action<(string MatchId, string Error)>                                                                     WaitingForMatchStateRunningError;
		public event Action<string>                                                                                             WaitingForMatchStateRunningCancelledWithMatchId;


		public event Action                                                                                                                        MatchmakingStarted;
		public event Action<(string MatchId, string TcpUdpServerAddress, string WebServerAddress, string UserSecret, List<string> MatchedPlayers)> MatchmakingFinished;
		public event Action<string>                                                                                                                MatchmakingError;
		public event Action                                                                                                                        MatchmakingCancelled;

		public RemoteMatchmakerClient(IUserApiClient userApiClient)
		{
			_userApiClient = userApiClient;

			LookingForUnfinishedMatchCancelled += _ => MatchmakingCancelled?.Invoke();
			WaitingForMatchCancelled += _ => MatchmakingCancelled?.Invoke();
			WaitingForMatchStateInitializingCancelledWithMatchId += _ => MatchmakingCancelled?.Invoke();
			WaitingForMatchStateRunningCancelledWithMatchId += _ => MatchmakingCancelled?.Invoke();

			LookingForUnfinishedMatchError += result => MatchmakingError?.Invoke(result.Error);
			WaitingForMatchError += result => MatchmakingError?.Invoke(result.Error);
			WaitingForMatchStateInitializingError += result => MatchmakingError?.Invoke(result.Error);
			WaitingForMatchStateRunningError += result => MatchmakingError?.Invoke(result.Error);
		}

		public void JoinMatchmakerAsync(string gameId, string gameVersion, bool tryReconnect, float[] matchmakerData = null, byte[] gameEngineData = null, string queueName = null, CancellationToken ct = default, string regionName = null)
		{
			MatchmakingStarted?.Invoke();

			void OnUnfinishedMatchResolved(string matchId)
			{
				if (!string.IsNullOrEmpty(matchId))
					OnMatchCreated(matchId);
				else
					WaitForMatch(OnMatchCreated, gameId, gameVersion, matchmakerData, gameEngineData, queueName, regionName, ct);
			}

			void OnMatchCreated(string matchId)
			{
				WaitForMatchState(OnMatchInitialized, matchId, GetMatchDesiredState.Initializing, ct);
			}

			void OnMatchInitialized((string MatchId, string TcpUdpServerAddress, string WebServerAddress, string UserSecret, List<string> MatchedPlayers) result)
			{
				WaitForMatchState(OnMatchRunning, result.MatchId, GetMatchDesiredState.Running, ct);
			}

			void OnMatchRunning((string MatchId, string TcpUdpServerAddress, string WebServerAddress, string UserSecret, List<string> MatchedPlayers) result)
			{
				MatchmakingFinished?.Invoke((result.MatchId, result.TcpUdpServerAddress, result.WebServerAddress, result.UserSecret, result.MatchedPlayers));
			}

			if (tryReconnect)
				GetFirstUnfinishedMatchId(OnUnfinishedMatchResolved, gameId, gameVersion, ct);
			else
				WaitForMatch(OnMatchCreated, gameId, gameVersion, matchmakerData, gameEngineData, queueName, regionName, ct);
		}

		private void GetFirstUnfinishedMatchId(Action<string> callback, string gameId, string gameVersion, CancellationToken ct)
		{
			LookingForUnfinishedMatchStarted?.Invoke((gameId, gameVersion));

			void OnUnfinishedMatchesResult(GetUnfinishedMatchesModel.Response response, Exception exception)
			{
				if (ct.IsCancellationRequested)
				{
					LookingForUnfinishedMatchCancelled?.Invoke((gameId, gameVersion));
					return;
				}

				if (exception != null)
				{
					LookingForUnfinishedMatchError?.Invoke((gameId, gameVersion, exception.Message));
					return;
				}

				if (response == null || !response.IsSuccess)
				{
					LookingForUnfinishedMatchError?.Invoke((gameId, gameVersion, response?.ErrorMessage));
					return;
				}

				var matchId = response.MatchesIds.Count > 0 ? response.MatchesIds[0] : null;
				LookingForUnfinishedMatchFinished?.Invoke((gameId, gameVersion, matchId));
				callback(matchId);
			}

			_userApiClient.GetUnfinishedMatchesAsync(new GetUnfinishedMatchesModel.Request(), OnUnfinishedMatchesResult, ct);
		}

		private void WaitForMatch(Action<string> callback, string gameId, string gameVersion, float[] matchmakerData, byte[] gameEngineData, string queueName, string regionName, CancellationToken ct)
		{
			var getPendingMatchRequest = new JoinMatchmakerAndWaitForMatchModel.Request
			{
				GameId = gameId,
				GameVersion = gameVersion,
				MatchmakerData = matchmakerData,
				GameEngineData = gameEngineData,
				QueueName = queueName,
				RegionName = regionName,
			};

			WaitingForMatchStarted?.Invoke((gameId, gameVersion));

			void OnMatchmakerJoinResult(JoinMatchmakerAndWaitForMatchModel.Response response, Exception exception)
			{
				if (ct.IsCancellationRequested)
				{
					WaitingForMatchCancelled?.Invoke((gameId, gameVersion));
					return;
				}

				if (exception != null)
				{
					WaitingForMatchError?.Invoke((gameId, gameVersion, exception.Message));
					return;
				}

				if (IsOpponentFound(response))
				{
					WaitingForMatchFinished?.Invoke((gameId, gameVersion, response.MatchId));
					callback(response.MatchId);
					return;
				}

				if (IsOpponentNotFound(response))
				{
					WaitingForMatchRetried?.Invoke((gameId, gameVersion));
					_userApiClient.JoinMatchmakerAndWaitForMatchAsync(getPendingMatchRequest, OnMatchmakerJoinResult, ct);
				}
				else
				{
					WaitingForMatchError?.Invoke((gameId, gameVersion, response == null ? "null response" : response.ErrorMessage));
				}
			}

			_userApiClient.JoinMatchmakerAndWaitForMatchAsync(getPendingMatchRequest, OnMatchmakerJoinResult, ct);
		}

		private static bool IsOpponentFound(JoinMatchmakerAndWaitForMatchModel.Response response)
		{
			return response != null && response.IsSuccess;
		}

		private static bool IsOpponentNotFound(JoinMatchmakerAndWaitForMatchModel.Response response)
		{
			return response != null && !response.IsSuccess && response.ErrorMessage == JoinMatchmakerAndWaitForMatchModel.ErrorCodes.OpponentNotFound;
		}

		private void WaitForMatchState(Action<(string MatchId, string TcpUdpServerAddress, string WebServerAddress, string UserSecret, List<string> MatchedPlayers)> callback, string matchId, GetMatchDesiredState desiredState, CancellationToken ct)
		{
			var getMatchRequest = new GetMatchModel.Request
			{
				MatchId = matchId,
				DesiredState = desiredState
			};

			switch (desiredState)
			{
				case GetMatchDesiredState.Initializing:
					WaitingForMatchStateInitializingStartedWithMatchId?.Invoke(matchId);
					break;
				case GetMatchDesiredState.Running:
					WaitingForMatchStateRunningStartedWithMatchId?.Invoke(matchId);
					break;
			}

			void OnMatchStateResult(GetMatchModel.Response response, Exception exception)
			{
				if (ct.IsCancellationRequested)
				{
					switch (desiredState)
					{
						case GetMatchDesiredState.Initializing:
							WaitingForMatchStateInitializingCancelledWithMatchId?.Invoke(matchId);
							break;
						case GetMatchDesiredState.Running:
							WaitingForMatchStateRunningCancelledWithMatchId?.Invoke(matchId);
							break;
					}

					return;
				}

				if (exception != null)
				{
					switch (desiredState)
					{
						case GetMatchDesiredState.Initializing:
							WaitingForMatchStateInitializingError?.Invoke((matchId, exception.Message));
							break;
						case GetMatchDesiredState.Running:
							WaitingForMatchStateRunningError?.Invoke((matchId, exception.Message));
							break;
					}

					return;
				}

				if (IsMatchInDesiredState(response))
				{
					switch (desiredState)
					{
						case GetMatchDesiredState.Initializing:
							WaitingForMatchStateInitializingFinishedWithMatchId?.Invoke(matchId);
							break;
						case GetMatchDesiredState.Running:
							WaitingForMatchStateRunningFinished?.Invoke((matchId, response.TcpUdpServerAddress, response.WebServerAddress, response.MatchedPlayersId));
							break;
					}

					callback((response.MatchId, response.TcpUdpServerAddress, response.WebServerAddress, response.UserSecret, response.MatchedPlayersId));
					return;
				}

				if (IsMatchNotInDesiredState(response))
				{
					switch (desiredState)
					{
						case GetMatchDesiredState.Initializing:
							WaitingForMatchStateInitializingRetriedWithMatchId?.Invoke(matchId);
							break;
						case GetMatchDesiredState.Running:
							WaitingForMatchStateRunningRetriedWithMatchId?.Invoke(matchId);
							break;
					}
				}
				else
				{
					switch (desiredState)
					{
						case GetMatchDesiredState.Initializing:
							WaitingForMatchStateInitializingError?.Invoke((matchId, response == null ? "null response" : response.ErrorMessage));
							break;
						case GetMatchDesiredState.Running:
							WaitingForMatchStateRunningError?.Invoke((matchId, response == null ? "null response" : response.ErrorMessage));
							break;
					}
				}
			}

			_userApiClient.GetMatchLongPollingAsync(getMatchRequest, OnMatchStateResult, ct);
		}

		private bool IsMatchInDesiredState(GetMatchModel.Response response)
		{
			return response != null && response.IsSuccess;
		}

		private bool IsMatchNotInDesiredState(GetMatchModel.Response response)
		{
			return response != null && !response.IsSuccess && response.ErrorMessage == GetMatchModel.ErrorCodes.NotInDesiredState;
		}
	}
}