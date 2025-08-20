using Elympics.ElympicsSystems.Internal;
using Elympics.Libraries;
using MatchTcpClients;

namespace Elympics
{
    internal class OnlineGameClientInitializer : GameClientInitializer
    {
        protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
        {
            if (ElympicsLobbyClient.Instance == null)
            {
                ElympicsLogger.LogError($"{nameof(ElympicsLobbyClient)} object is not present. "
                    + "Make sure to setup a menu scene before playing an online match.");
                return;
            }
            if (!ElympicsLobbyClient.Instance.IsAuthenticated)
            {
                ElympicsLogger.LogError("User is not authenticated. Remember to set \"Authenticate On Awake With\" "
                    + $"correctly for {nameof(ElympicsLobbyClient)} object.");
                return;
            }
            var matchData = ElympicsLobbyClient.Instance.MatchDataGuid;
            if (matchData == null)
            {
                ElympicsLogger.LogError("Match data not found. Going through matchmaking is required "
                    + $"before joining an online match. See {nameof(ElympicsLobbyClient)}."
                    + $"{nameof(ElympicsLobbyClient.Instance)}.{nameof(ElympicsLobbyClient.PlayOnlineInRegion)}().");
                return;
            }

            var userId = ElympicsLobbyClient.Instance.UserGuid.Value;
            var matchmakerData = matchData.MatchmakerData;
            var gameEngineData = matchData.GameEngineData;
            var player = ElympicsPlayerAssociations.GetUserIdsToPlayers(matchData.MatchedPlayers)[userId];

            var serializer = new GameServerJsonSerializer();
            var config = elympicsGameConfig.ConnectionConfig.GameServerClientConfig;
            var gsEndpoint = ElympicsConfig.Load().ElympicsGameServersEndpoint;
            var webSignalingEndpoint = WebGameServerClient.GetSignalingServerBaseAddress(gsEndpoint, matchData.WebServerAddress, matchData.RegionName);
            var gameLogger = ElympicsLogger.CurrentContext!.Value.SetGameMode("online").WithApp(ElympicsLoggerContext.GameplayContextApp);
            var gameServerClient = elympicsGameConfig.UseWeb
                ? (GameServerClient)new WebGameServerClient(serializer, config,
                    new HttpSignalingClient(webSignalingEndpoint, matchData.MatchId),
                    gameLogger,
                    WebRtcFactory.CreateInstance)
                : new TcpUdpGameServerClient(serializer, config,
                    IPEndPointExtensions.Parse(matchData.TcpUdpServerAddress),
                    gameLogger);
            var matchConnectClient = new RemoteMatchConnectClient(gameServerClient,
                gameLogger,
                matchData.TcpUdpServerAddress, matchData.WebServerAddress, matchData.UserSecret,
                elympicsGameConfig.UseWeb);
            var matchClient = new RemoteMatchClient(gameServerClient, elympicsGameConfig);
            elympicsGameConfig.players = matchData.MatchedPlayers.Length;
            client.InitializeInternal(elympicsGameConfig, matchConnectClient, matchClient, new InitialMatchPlayerDataGuid(player, gameEngineData, matchmakerData)
            {
                UserId = userId,
                IsBot = false,
            }, ElympicsBehavioursManager, gameLogger);
        }
    }
}
