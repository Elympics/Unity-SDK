using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.Libraries;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using MatchTcpClients;
using Plugins.Elympics.Plugins.ParrelSync;

namespace Elympics
{
    internal class DebugOnlineClientInitializer : GameClientInitializer
    {
        private static readonly TimeSpan MatchmakingTimeout = TimeSpan.FromSeconds(60);

        private ElympicsClient _client;

        private IAuthClient _authClient;
        private MatchmakerClient _matchmakerClient;

        private ElympicsGameConfig _elympicsGameConfig;
        private InitialMatchPlayerDataGuid _initialPlayerData;

        protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
        {
            _client = client;
            _elympicsGameConfig = elympicsGameConfig;
            var elympicsConfig = ElympicsConfig.Load();

            _authClient = new RemoteAuthClient(elympicsConfig.ElympicsAuthEndpoint);
            _matchmakerClient = new WebSocketMatchmakerClient(elympicsConfig.ElympicsLobbyEndpoint);
            _matchmakerClient.MatchmakingSucceeded += OnMatchmakingSucceeded;
            _matchmakerClient.MatchmakingMatchFound += matchId => ElympicsLogger.Log($"Match found: {matchId}.");
            _matchmakerClient.MatchmakingFailed += args => ElympicsLogger.LogError($"Matchmaking error: {args.Error}");

            var playerIndex = ElympicsClonesManager.IsClone() ? ElympicsClonesManager.GetCloneNumber() + 1 : 0;
            ElympicsGameConfig.InitialUserData testPlayerData;
            if (_elympicsGameConfig.TestPlayers.Count > playerIndex)
                testPlayerData = _elympicsGameConfig.TestPlayers[playerIndex];
            else
            {
                testPlayerData = new ElympicsGameConfig.InitialUserData();
                ElympicsLogger.LogWarning("Using empty initial user data, " + $"because no data for player ID: {playerIndex} in \"Test players\" list. " + $"The list has only {_elympicsGameConfig.TestPlayers.Count} entries. " + $"Try increasing \"Players\" count in your {nameof(ElympicsGameConfig)}.");
            }
            _initialPlayerData = new InitialMatchPlayerDataGuid(ElympicsPlayer.FromIndex(playerIndex), testPlayerData.gameEngineData, testPlayerData.matchmakerData);
        }

        private async UniTask Connect()
        {
            ElympicsLogger.LogWarning($"Starting {AuthType.ClientSecret} authentication...");
            try
            {
                var clientSecret = ElympicsLobbyClient.GetOrCreateClientSecret();
                var results = await _authClient.AuthenticateWithClientSecret(clientSecret);
                OnAuthenticated(results);
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException(e);
            }
        }

        private void OnAuthenticated(Result<AuthData, string> result)
        {
            if (result.IsFailure)
            {
                ElympicsLogger.LogError($"Connecting failed: {result.Error}");
                return;
            }

            _initialPlayerData.UserId = result.Value.UserId;
            ElympicsLogger.Log($"{AuthType.ClientSecret} authentication successful with user id: {_initialPlayerData.UserId}.");

            var cts = new CancellationTokenSource(MatchmakingTimeout);
            var testMatchData = _elympicsGameConfig.TestMatchData;
            var regionName = testMatchData.regionName;
            if (string.IsNullOrEmpty(regionName))
                regionName = null;

            ElympicsLogTemplates.LogJoiningMatchmaker(_initialPlayerData.UserId, _initialPlayerData.MatchmakerData, _initialPlayerData.GameEngineData, testMatchData.queueName, regionName, false);

            _matchmakerClient.JoinMatchmakerAsync(new JoinMatchmakerData
            {
                GameId = new Guid(_elympicsGameConfig.GameId),
                GameVersion = _elympicsGameConfig.GameVersion,
                QueueName = testMatchData.queueName,
                RegionName = regionName,
                GameEngineData = _initialPlayerData.GameEngineData,
                MatchmakerData = _initialPlayerData.MatchmakerData,
            },
            result.Value,
            cts.Token);
        }

        private void OnMatchmakingSucceeded(MatchmakingFinishedData matchData)
        {
            const string gameModeName = "debug-online-client";

            ElympicsLogger.Log("Matchmaking finished, connecting to the game server...");
            _initialPlayerData.Player = ElympicsPlayerAssociations.GetUserIdsToPlayers(matchData.MatchedPlayers)[_initialPlayerData.UserId];

            var serializer = new GameServerJsonSerializer();
            var config = _elympicsGameConfig.ConnectionConfig.GameServerClientConfig;
            var gsEndpoint = ElympicsConfig.Load().ElympicsGameServersEndpoint;
            var webSignalingEndpoint = WebGameServerClient.GetSignalingServerBaseAddress(gsEndpoint, matchData.WebServerAddress, _elympicsGameConfig.TestMatchData.regionName);
            var logger = ElympicsLogger.CurrentContext ?? new ElympicsLoggerContext(Guid.NewGuid());
            logger = logger.SetGameMode(gameModeName).WithApp(ElympicsLoggerContext.GameplayContextApp).SetElympicsContext(ElympicsConfig.SdkVersion, _elympicsGameConfig.gameId);
            GameServerClient gameServerClient = _elympicsGameConfig.UseWeb
                ? new WebGameServerClient(serializer, config, new HttpSignalingClient(webSignalingEndpoint, matchData.MatchId), logger, WebRtcFactory.CreateInstance)
                : new TcpUdpGameServerClient(serializer, config, IPEndPointExtensions.Parse(matchData.TcpUdpServerAddress), logger);
            var matchConnectClient = new RemoteMatchConnectClient(gameServerClient, logger, matchData.TcpUdpServerAddress, matchData.WebServerAddress, matchData.UserSecret, _elympicsGameConfig.UseWeb);
            var matchClient = new RemoteMatchClient(gameServerClient, _elympicsGameConfig);
            _elympicsGameConfig.players = matchData.MatchedPlayers.Length;
            _client.InitializeInternal(_elympicsGameConfig,
            matchConnectClient,
            matchClient,
            new InitialMatchPlayerDataGuid(_initialPlayerData.Player, _initialPlayerData.GameEngineData, _initialPlayerData.MatchmakerData)
            {
                UserId = _initialPlayerData.UserId,
                IsBot = false,
            },
            ElympicsBehavioursManager,
            logger);
        }
    }
}
