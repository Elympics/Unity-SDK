using Elympics.Libraries;
using Elympics.Models.Authentication;
using MatchTcpClients;
using UnityEngine;

namespace Elympics
{
	internal class OnlineGameClientInitializer : GameClientInitializer
	{
		protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
		{
			var matchData = ElympicsLobbyClient.Instance.MatchDataGuid;
			if (matchData == null)
			{
				Debug.LogError("[Elympics] Match data not found. Did you try to join an online match without going through matchmaking first?");
				return;
			}
			if (!ElympicsLobbyClient.Instance.IsAuthenticated)
			{
				Debug.LogError("[Elympics] User is not authenticated. Did you try to join an online match without going through matchmaking first?");
				return;
			}

			var userId = ElympicsLobbyClient.Instance.UserGuid.Value;
			var matchmakerData = matchData.MatchmakerData;
			var gameEngineData = matchData.GameEngineData;
			var player = ElympicsPlayerAssociations.GetUserIdsToPlayers(matchData.MatchedPlayers)[userId];

			var logger = new LoggerDebug();
			var serializer = new GameServerJsonSerializer();
			var config = elympicsGameConfig.ConnectionConfig.GameServerClientConfig;
			var gsEndpoint = ElympicsConfig.Load().ElympicsGameServersEndpoint;
			var webSignalingEndpoint = WebGameServerClient.GetSignalingEndpoint(gsEndpoint, matchData.WebServerAddress,
				matchData.MatchId.ToString());
			var gameServerClient = elympicsGameConfig.UseWeb
				? (GameServerClient)new WebGameServerClient(logger, serializer, config,
					new HttpSignalingClient(webSignalingEndpoint),
					WebRtcFactory.CreateInstance)
				: new TcpUdpGameServerClient(logger, serializer, config,
					IPEndPointExtensions.Parse(matchData.TcpUdpServerAddress));
			var matchConnectClient = new RemoteMatchConnectClient(gameServerClient, matchData.MatchId.ToString(),
				matchData.TcpUdpServerAddress, matchData.WebServerAddress, matchData.UserSecret,
				elympicsGameConfig.UseWeb, matchData.RegionName);
			var matchClient = new RemoteMatchClient(gameServerClient, elympicsGameConfig);

			client.InitializeInternal(elympicsGameConfig, matchConnectClient, matchClient, new InitialMatchPlayerDataGuid
			{
				Player = player,
				UserId = userId,
				IsBot = false,
				MatchmakerData = matchmakerData,
				GameEngineData = gameEngineData
			});
		}
	}
}
