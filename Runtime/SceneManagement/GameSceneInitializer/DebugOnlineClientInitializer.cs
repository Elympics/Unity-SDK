using System;
using System.Threading;
using Elympics.Libraries;
using Elympics.Models.Authentication;
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

        private IAuthClient _authClient;
        private MatchmakerClient _matchmakerClient;

        private ElympicsGameConfig _elympicsGameConfig;
        private InitialMatchPlayerDataGuid _initialPlayerData;

        protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
        {
            _client = client;
            _elympicsGameConfig = elympicsGameConfig;

            _authClient = new RemoteAuthClient();
            _matchmakerClient = MatchmakerClientFactory.Create(_elympicsGameConfig, ElympicsConfig.Load().ElympicsLobbyEndpoint);
            _matchmakerClient.MatchmakingSucceeded += OnMatchmakingSucceeded;
            _matchmakerClient.MatchmakingMatchFound += matchId => Debug.Log($"Match found: {matchId}");
            _matchmakerClient.MatchmakingFailed += args => Debug.LogError($"Matchmaking error: {args.Error}");

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
                _authClient.AuthenticateWithClientSecret(clientSecret, OnAuthenticated);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("{0} \n {1}", e.Message, e.StackTrace);
            }
        }

        private void OnAuthenticated(Result<AuthData, string> result)
        {
            if (result.IsFailure)
            {
                Debug.LogError($"Connecting failed: {result.Error}");
                return;
            }

            _initialPlayerData.UserId = result.Value.UserId;

            var cts = new CancellationTokenSource(MatchmakingTimeout);
            var regionName = _elympicsGameConfig.TestMatchData.regionName;
            if (string.IsNullOrEmpty(regionName))
                regionName = null;
            _matchmakerClient.JoinMatchmakerAsync(new JoinMatchmakerData
            {
                GameId = new Guid(_elympicsGameConfig.GameId),
                GameVersion = _elympicsGameConfig.GameVersion,
                QueueName = _elympicsGameConfig.TestMatchData.queueName,
                RegionName = regionName,
                GameEngineData = _initialPlayerData.GameEngineData,
                MatchmakerData = _initialPlayerData.MatchmakerData,
            }, result.Value, cts.Token);
        }

        private void OnMatchmakingSucceeded(MatchmakingFinishedData matchData)
        {
            Debug.Log("Matchmaking finished, connecting to the game server...");
            _initialPlayerData.Player = ElympicsPlayerAssociations.GetUserIdsToPlayers(matchData.MatchedPlayers)[_initialPlayerData.UserId];

            var logger = new LoggerDebug();
            var serializer = new GameServerJsonSerializer();
            var config = _elympicsGameConfig.ConnectionConfig.GameServerClientConfig;
            var gsEndpoint = ElympicsConfig.Load().ElympicsGameServersEndpoint;
            var webSignalingEndpoint = WebGameServerClient.GetSignalingEndpoint(gsEndpoint, matchData.WebServerAddress,
                matchData.MatchId.ToString(), _elympicsGameConfig.TestMatchData.regionName);
            var gameServerClient = _elympicsGameConfig.UseWeb
                ? (GameServerClient)new WebGameServerClient(logger, serializer, config,
                    new HttpSignalingClient(webSignalingEndpoint),
                    WebRtcFactory.CreateInstance)
                : new TcpUdpGameServerClient(logger, serializer, config,
                    IPEndPointExtensions.Parse(matchData.TcpUdpServerAddress));

            var matchConnectClient = new RemoteMatchConnectClient(gameServerClient,
                matchData.TcpUdpServerAddress, matchData.WebServerAddress, matchData.UserSecret,
                _elympicsGameConfig.UseWeb);
            var matchClient = new RemoteMatchClient(gameServerClient, _elympicsGameConfig);
            _elympicsGameConfig.players = matchData.MatchedPlayers.Length;
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
