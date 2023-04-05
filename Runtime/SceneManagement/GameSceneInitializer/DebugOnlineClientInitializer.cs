using System;
using System.Threading;
using Elympics.Libraries;
using Elympics.Models.Matchmaking;
using MatchTcpClients;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEngine;

namespace Elympics
{
	internal class DebugOnlineClientInitializer : GameClientInitializer
	{
		private static readonly TimeSpan MatchmakingTimeout = TimeSpan.FromSeconds(60);

		private ElympicsClient _client;

		private IAuthenticationClient _authClient;
		private MatchmakerClient      _matchmakerClient;
		private IUserApiClient        _userApiClient;

		private ElympicsGameConfig         _elympicsGameConfig;
		private InitialMatchPlayerDataGuid _initialPlayerData;

		protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
		{
			_client = client;
			_elympicsGameConfig = elympicsGameConfig;

			_userApiClient = new UserApiClient();
			_authClient = new RemoteAuthenticationClient(_userApiClient);
			_matchmakerClient = MatchmakerClientFactory.Create(_elympicsGameConfig, _userApiClient);

			var playerIndex = ElympicsClonesManager.IsClone() ? ElympicsClonesManager.GetCloneNumber() + 1 : 0;
			var testPlayerData = _elympicsGameConfig.TestPlayers[playerIndex];
			_initialPlayerData = new InitialMatchPlayerDataGuid
			{
				Player = ElympicsPlayer.FromIndex(playerIndex),
				GameEngineData = testPlayerData.gameEngineData,
				MatchmakerData = testPlayerData.matchmakerData
			};
			Connect();
		}

		private void Connect()
		{
			try
			{
				var clientSecret = Guid.NewGuid().ToString();
				_authClient.AuthenticateWithClientSecret(ElympicsConfig.Load().ElympicsLobbyEndpoint, clientSecret, OnAuthenticated);
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("{0} \n {1}", e.Message, e.StackTrace);
			}
		}

		private void OnAuthenticated((bool Success, Guid UserId, string JwtToken, string Error) result)
		{
			if (!result.Success)
			{
				Debug.LogError($"Connecting failed: {result.Error}");
				return;
			}

			_initialPlayerData.UserId = result.UserId;

			var cts = new CancellationTokenSource(MatchmakingTimeout);
			_matchmakerClient.MatchmakingSucceeded += OnMatchmakingSucceeded;
			_matchmakerClient.MatchmakingMatchFound += matchId => Debug.Log($"Match found: {matchId}");
			_matchmakerClient.MatchmakingFailed += args => Debug.LogError($"Matchmaking error: {args.Error}");
			_matchmakerClient.JoinMatchmakerAsync(new JoinMatchmakerData
			{
				GameId = new Guid(_elympicsGameConfig.GameId),
				GameVersion = _elympicsGameConfig.GameVersion,
				QueueName = _elympicsGameConfig.TestMatchData.queueName,
				RegionName = _elympicsGameConfig.TestMatchData.regionName,
				GameEngineData = _initialPlayerData.GameEngineData,
				MatchmakerData = _initialPlayerData.MatchmakerData,
			}, cts.Token);
		}

		private void OnMatchmakingSucceeded(MatchmakingFinishedData matchData)
		{
			Debug.Log("Matchmaking finished, connecting to the game server...");
			_initialPlayerData.Player = ElympicsPlayerAssociations.GetUserIdsToPlayers(matchData.MatchedPlayers)[_initialPlayerData.UserId];

			var logger = new LoggerDebug();
			var serializer = new GameServerJsonSerializer();
			var config = _elympicsGameConfig.ConnectionConfig.GameServerClientConfig;
			var gsEndpoint = ElympicsConfig.Load().ElympicsGameServersEndpoint;
			var webSignalingEndpoint = WebGameServerClient.GetSignalingEndpoint(gsEndpoint, matchData.WebServerAddress, matchData.MatchId.ToString());
			var gameServerClient = _elympicsGameConfig.UseWeb
				? (GameServerClient)new WebGameServerClient(logger, serializer, config,
					new HttpSignalingClient(webSignalingEndpoint),
					WebRtcFactory.CreateInstance)
				: new TcpUdpGameServerClient(logger, serializer, config,
					IPEndPointExtensions.Parse(matchData.TcpUdpServerAddress));

			var matchConnectClient = new RemoteMatchConnectClient(gameServerClient, matchData.MatchId.ToString(),
				matchData.TcpUdpServerAddress, matchData.WebServerAddress, matchData.UserSecret,
				_elympicsGameConfig.UseWeb, _elympicsGameConfig.TestMatchData.regionName);
			var matchClient = new RemoteMatchClient(gameServerClient, _elympicsGameConfig);

			_client.InitializeInternal(_elympicsGameConfig, matchConnectClient, matchClient, new InitialMatchPlayerDataGuid
			{
				Player = _initialPlayerData.Player,
				UserId = _initialPlayerData.UserId,
				IsBot = false,
				MatchmakerData = _initialPlayerData.MatchmakerData,
				GameEngineData = _initialPlayerData.GameEngineData
			});
		}
	}
}
