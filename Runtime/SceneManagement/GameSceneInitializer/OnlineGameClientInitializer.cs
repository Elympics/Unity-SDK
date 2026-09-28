using System;
using Elympics.Core.Logger;
using Elympics.Models.Matchmaking;
using MatchTcpClients;

namespace Elympics
{
    internal class OnlineGameClientInitializer : GameClientInitializer
    {
        private readonly MatchmakingFinishedData _matchData;
        private readonly Guid _userId;

        public OnlineGameClientInitializer(MatchmakingFinishedData matchData, Guid userId)
        {
            _matchData = matchData;
            _userId = userId;
        }

        protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
        {
            var matchData = _matchData;
            var userId = _userId;
            var matchmakerData = matchData.MatchmakerData;
            var gameEngineData = matchData.GameEngineData;
            var player = ElympicsPlayerAssociations.GetUserIdsToPlayers(matchData.MatchedPlayers)[userId];

            var serializer = new GameServerJsonSerializer();
            var config = elympicsGameConfig.ConnectionConfig.GameServerClientConfig;
            var gsEndpoint = ElympicsConfig.Load().ElympicsGameServersEndpoint;
            var webSignalingEndpoint = WebGameServerClient.GetSignalingServerBaseAddress(gsEndpoint, matchData.WebServerAddress, matchData.RegionName);
            ElympicsLogger.State.SetGameMode("online");
            GameServerClient gameServerClient = elympicsGameConfig.UseWeb
                ? new WebGameServerClient(serializer,
                    config,
                    new HttpSignalingClient(webSignalingEndpoint, matchData.MatchId, config))
                : new TcpUdpGameServerClient(serializer,
                    config,
                    IPEndPointExtensions.Parse(matchData.TcpUdpServerAddress));
            var matchConnectClient = new RemoteMatchConnectClient(gameServerClient, matchData.UserSecret);
            var matchClient = new RemoteMatchClient(gameServerClient, elympicsGameConfig);
            var matchPlayerCount = matchData.MatchedPlayers.Length;
            if (matchPlayerCount > elympicsGameConfig.MaxPlayers)
                throw new ElympicsException(
                    $"Match player count ({matchPlayerCount}) exceeds configured {nameof(ElympicsGameConfig.MaxPlayers)} ({elympicsGameConfig.MaxPlayers}).");
            client.InitializeInternal(elympicsGameConfig,
                matchConnectClient,
                matchClient,
                new InitialMatchPlayerDataGuid(player, gameEngineData, matchmakerData)
                {
                    UserId = userId,
                    IsBot = false,
                },
                ElympicsBehavioursManager,
                elympicsGameConfig.MaxPlayers);
        }
    }
}
