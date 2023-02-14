using System;
using System.Collections.Generic;
using Elympics.Libraries;
using MatchTcpClients;
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
				_myMatchmakerClient.JoinMatchmakerAsync(_elympicsGameConfig.GameId, _elympicsGameConfig.GameVersion,
					false, _matchmakerData, _gameEngineData, _elympicsGameConfig.TestMatchData.queueName, default,
					_elympicsGameConfig.TestMatchData.regionName);
			}
			else
			{
				Debug.LogError("Connecting failed");
			}
		}

		private void OnMatchmakingFinished((string MatchId, string TcpUdpServerAddress, string WebServerAddress, string UserSecret, List<string> MatchedPlayers) _)
		{
			var matchData = _myMatchmakerClient.MatchData;
			var player = ElympicsPlayerAssociations.GetUserIdsToPlayers(matchData.MatchedPlayers)[_myUserId];

			var logger = new LoggerDebug();
			var serializer = new GameServerJsonSerializer();
			var config = _elympicsGameConfig.ConnectionConfig.GameServerClientConfig;
			var gsEndpoint = ElympicsConfig.Load().ElympicsGameServersEndpoint;
			var webSignalingEndpoint = WebGameServerClient.GetSignalingEndpoint(gsEndpoint, matchData.WebServerAddress, matchData.MatchId);
			var gameServerClient = _elympicsGameConfig.UseWeb
				? (GameServerClient)new WebGameServerClient(logger, serializer, config,
					new HttpSignalingClient(webSignalingEndpoint),
					WebRtcFactory.CreateInstance)
				: new TcpUdpGameServerClient(logger, serializer, config,
					IPEndPointExtensions.Parse(matchData.TcpUdpServerAddress));

			var matchConnectClient = new RemoteMatchConnectClient(gameServerClient, matchData.MatchId,
				matchData.TcpUdpServerAddress, matchData.WebServerAddress, matchData.UserSecret,
				_elympicsGameConfig.UseWeb, matchData.RegionName);
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
