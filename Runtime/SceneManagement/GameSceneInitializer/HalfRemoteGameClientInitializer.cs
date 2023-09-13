using System;
using static Elympics.ApplicationParameters.HalfRemote;

namespace Elympics
{
    internal class HalfRemoteGameClientInitializer : GameClientInitializer
    {
        private HalfRemoteMatchClientAdapter _halfRemoteMatchClient;
        private HalfRemoteMatchConnectClient _halfRemoteMatchConnectClient;

        protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
        {
            var playerIndex = GetPlayerIndex(elympicsGameConfig);
            var ip = GetIp(elympicsGameConfig);
            var port = GetPort(elympicsGameConfig);

            var playersList = DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig);

            if (playersList.Count <= playerIndex)
                throw ElympicsLogger.LogException("Half Remote client won't be initialized because "
                    + $"no data for player ID: {playerIndex} was found in \"Test players\" list. "
                    + $"The list has only {playersList.Count} entries. "
                    + $"Try increasing \"Players\" count in your {nameof(ElympicsGameConfig)}.");

            var userId = playersList[playerIndex].UserId;
            var matchmakerData = playersList[playerIndex].MatchmakerData;
            var gameEngineData = playersList[playerIndex].GameEngineData;

            _halfRemoteMatchClient = new HalfRemoteMatchClientAdapter(elympicsGameConfig);
            _halfRemoteMatchConnectClient = new HalfRemoteMatchConnectClient(_halfRemoteMatchClient, ip, port, new Guid(userId), elympicsGameConfig.UseWebInHalfRemote);
            client.InitializeInternal(elympicsGameConfig, _halfRemoteMatchConnectClient, _halfRemoteMatchClient,
                new InitialMatchPlayerDataGuid
                {
                    Player = ElympicsPlayer.FromIndex(playerIndex),
                    UserId = new Guid(userId),
                    IsBot = false,
                    MatchmakerData = matchmakerData,
                    GameEngineData = gameEngineData
                });
        }

        public override void Dispose()
        {
            _halfRemoteMatchConnectClient?.Dispose();
        }
    }
}
