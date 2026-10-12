using System.Linq;
using Elympics.Core.Logger;
using MatchTcpClients;

namespace Elympics
{
    internal class OnlineGameClientInitializer : GameClientInitializer
    {
        private readonly OnlineLaunchData _data;

        public OnlineGameClientInitializer(OnlineLaunchData data) => _data = data;

        protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
        {
            var data = _data;
            var matchedPlayers = data.MatchedPlayers.ToArray();
            var player = ElympicsPlayerAssociations.GetUserIdsToPlayers(matchedPlayers)[data.UserId];

            var serializer = new GameServerJsonSerializer();
            var config = elympicsGameConfig.ConnectionConfig.GameServerClientConfig;
            ElympicsLogger.State.SetGameMode("online");
            GameServerClient gameServerClient;
            if (data.UsesTcpUdp(elympicsGameConfig))
                gameServerClient = new TcpUdpGameServerClient(serializer, config, IPEndPointExtensions.Parse(data.ServerAddress));
            else
            {
                var gsEndpoint = ElympicsConfig.Load().ElympicsGameServersEndpoint;
                var webSignalingEndpoint = WebGameServerClient.GetSignalingServerBaseAddress(gsEndpoint, data.ServerAddress, data.RegionName);
                gameServerClient = new WebGameServerClient(serializer, config, new HttpSignalingClient(webSignalingEndpoint, data.MatchId, config));
            }
            var matchConnectClient = new RemoteMatchConnectClient(gameServerClient, data.UserSecret);
            var matchClient = new RemoteMatchClient(gameServerClient, elympicsGameConfig);
            client.InitializeInternal(elympicsGameConfig,
                matchConnectClient,
                matchClient,
                new InitialMatchPlayerDataGuid(player, data.GameEngineData.ToArray(), data.MatchmakerData.ToArray())
                {
                    UserId = data.UserId,
                    IsBot = false,
                },
                ElympicsBehavioursManager,
                elympicsGameConfig.MaxPlayers);
        }
    }
}
