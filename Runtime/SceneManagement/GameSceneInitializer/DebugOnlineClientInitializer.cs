using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elympics.Libraries;
using MatchTcpClients;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
	internal class DebugOnlineClientInitializer : GameClientInitializer
	{
		private ElympicsClient _client;

		private RemoteAuthenticationClient _myAuthenticationClient;
		private RemoteMatchmakerClient     _myMatchmakerClient;

		private ElympicsGameConfig _elympicsGameConfig;

		private string _myUserId;

		private float[] _matchmakerData = null;
		private byte[]  _gameEngineData = null;

		protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
		{
			_client = client;

			var lobbyPublicApiClient = new UserApiClient();
			_myAuthenticationClient = new RemoteAuthenticationClient(lobbyPublicApiClient);
			_myMatchmakerClient = new RemoteMatchmakerClient(lobbyPublicApiClient);

			_elympicsGameConfig = elympicsGameConfig;
			Connect();
		}

		private void Connect()
		{
			try
			{
				var userSecret = Guid.NewGuid().ToString();
				_myAuthenticationClient.AuthenticateWithAuthTokenAsync(ElympicsConfig.Load().ElympicsLobbyEndpoint, userSecret, OnAuthenticated);
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("{0} \n {1}", e.Message, e.StackTrace);
			}
		}


		private void OnAuthenticated((bool Success, string UserId, string JwtToken, string Error) result)
		{
			if (result.Success)
			{
				_myUserId = result.UserId;
				_myMatchmakerClient.MatchmakingFinished += OnMatchmakingFinished;
				_myMatchmakerClient.MatchmakingError += error => Debug.Log($"Matchmaking error - {error}");
				_myMatchmakerClient.JoinMatchmakerAsync(_elympicsGameConfig.GameId, _elympicsGameConfig.GameVersion, false, _matchmakerData, _gameEngineData);
			}
			else
			{
				Debug.LogError("Connecting failed");
			}
		}

		private void OnMatchmakingFinished((string MatchId, string TcpUdpServerAddress, string WebServerAddress, string UserSecret, List<string> MatchedPlayers) matchData)
		{
			var player = ElympicsPlayerAssociations.GetUserIdsToPlayers(matchData.MatchedPlayers)[_myUserId];

			var gameServerClient = new GameServerClient(
				new LoggerDebug(),
				new GameServerJsonSerializer(),
				new ClientSynchronizerConfig
				{
					// Todo use config ~pprzestrzelski 11.03.2021
					TimeoutTime = TimeSpan.FromSeconds(10),
					ContinuousSynchronizationMinimumInterval = TimeSpan.FromSeconds(1)
				}
			);
			gameServerClient.OverrideWebFactories(WebRtcFactory.CreateInstance);

			var matchConnectClient = new RemoteMatchConnectClient(gameServerClient, matchData.MatchId, matchData.TcpUdpServerAddress, matchData.WebServerAddress, matchData.UserSecret, _elympicsGameConfig.UseWeb);
			var matchClient = new RemoteMatchClient(gameServerClient, _elympicsGameConfig);

			_client.InitializeInternal(_elympicsGameConfig, matchConnectClient, matchClient, new InitialMatchPlayerData
			{
				Player = player,
				UserId = _myUserId,
				IsBot = false,
				MatchmakerData = _matchmakerData,
				GameEngineData = _gameEngineData
			});
		}
	}
}
