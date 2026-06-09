using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
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
            var lobbyUrl = elympicsConfig.ElympicsLobbyEndpoint;
            _ = ElympicsLogger.CurrentContext.SetLobbyUrl(lobbyUrl);
            _matchmakerClient = new WebSocketMatchmakerClient(lobbyUrl);
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
                ElympicsLogger.LogWarning("Using empty initial user data, "
                    + $"because no data for player ID: {playerIndex} in \"Test players\" list. "
                    + $"The list has only {_elympicsGameConfig.TestPlayers.Count} entries. "
                    + $"Try increasing \"Players\" count in your {nameof(ElympicsGameConfig)}.");
            }

            _initialPlayerData = new InitialMatchPlayerDataGuid(ElympicsPlayer.FromIndex(playerIndex), testPlayerData.gameEngineData, testPlayerData.matchmakerData);
            Connect().Forget();
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
            ElympicsLogger.CurrentContext.SetUserId(result.Value.UserId.ToString())
                .SetAuthType(result.Value.AuthType)
                .Log($"{AuthType.ClientSecret} authentication successful with user id: {_initialPlayerData.UserId}.");

            var cts = new CancellationTokenSource(MatchmakingTimeout);
            var testMatchData = _elympicsGameConfig.TestMatchData;
            var queueName = testMatchData.queueName;
            var regionName = testMatchData.regionName;
            if (string.IsNullOrEmpty(regionName))
                regionName = null;
            _ = ElympicsLogger.CurrentContext.SetQueue(queueName)
                .SetRegion(regionName);

            ElympicsLogTemplates.LogJoiningMatchmaker(_initialPlayerData.UserId, _initialPlayerData.MatchmakerData, _initialPlayerData.GameEngineData, queueName, regionName, false);

            _matchmakerClient.JoinMatchmakerAsync(new JoinMatchmakerData
            {
                GameId = new Guid(_elympicsGameConfig.GameId),
                GameVersion = _elympicsGameConfig.GameVersion,
                QueueName = queueName,
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

            ElympicsLogger.CurrentContext.SetMatchId(matchData.MatchId.ToString())
                .SetServerAddress(matchData.TcpUdpServerAddress, matchData.WebServerAddress)
                .Log("Matchmaking finished, connecting to the game server...");
            _initialPlayerData.Player = ElympicsPlayerAssociations.GetUserIdsToPlayers(matchData.MatchedPlayers)[_initialPlayerData.UserId];

            var serializer = new GameServerJsonSerializer();
            var config = _elympicsGameConfig.ConnectionConfig.GameServerClientConfig;
            var gsEndpoint = ElympicsConfig.Load().ElympicsGameServersEndpoint;
            var webSignalingEndpoint = WebGameServerClient.GetSignalingServerBaseAddress(gsEndpoint, matchData.WebServerAddress, _elympicsGameConfig.TestMatchData.regionName);
            _ = ElympicsLogger.CurrentContext.SetGameMode(gameModeName)
                .SetElympicsContext(ElympicsConfig.SdkVersion, _elympicsGameConfig.gameId);
            GameServerClient gameServerClient = _elympicsGameConfig.UseWeb
                ? new WebGameServerClient(serializer,
                    config,
                    new HttpSignalingClient(webSignalingEndpoint, matchData.MatchId, config))
                : new TcpUdpGameServerClient(serializer, config, IPEndPointExtensions.Parse(matchData.TcpUdpServerAddress));
            var matchConnectClient = new RemoteMatchConnectClient(gameServerClient, matchData.UserSecret);
            var matchClient = new RemoteMatchClient(gameServerClient, _elympicsGameConfig);
            var matchPlayerCount = matchData.MatchedPlayers.Length;
            if (matchPlayerCount > _elympicsGameConfig.MaxPlayers)
                throw new ElympicsException($"Match player count ({matchPlayerCount}) exceeds configured {nameof(ElympicsGameConfig.MaxPlayers)} ({_elympicsGameConfig.MaxPlayers}).");
            _client.InitializeInternal(_elympicsGameConfig,
                matchConnectClient,
                matchClient,
                new InitialMatchPlayerDataGuid(_initialPlayerData.Player, _initialPlayerData.GameEngineData, _initialPlayerData.MatchmakerData)
                {
                    UserId = _initialPlayerData.UserId,
                    IsBot = false,
                },
                ElympicsBehavioursManager,
                matchPlayerCount);
        }
    }
}
