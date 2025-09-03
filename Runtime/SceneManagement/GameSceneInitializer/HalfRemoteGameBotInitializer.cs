using System.Linq;
using System.Threading;
using GameBotCore.V1._3;

namespace Elympics
{
    internal class HalfRemoteGameBotInitializer : GameBotInitializer
    {
        private HalfRemoteMatchClientAdapter _halfRemoteMatchClient;
        private HalfRemoteMatchConnectClient _halfRemoteMatchConnectClient;

        protected override void InitializeBot(ElympicsBot bot, ElympicsGameConfig elympicsGameConfig, GameBotAdapter gameBotAdapter)
        {
            var playerIndex = elympicsGameConfig.PlayerIndexForHalfRemoteMode;
            var playersList = DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig);

            if (playersList.Count > playerIndex)
                throw ElympicsLogger.LogException("Half Remote bot won't be initialized because "
                    + $"no data for player ID: {playerIndex} was found in \"Test players\" list. "
                    + $"The list has only {playersList.Count} entries. "
                    + $"Try increasing \"Players\" count in your {nameof(ElympicsGameConfig)}.");

            var userId = playersList[playerIndex].UserId;

            var botConfiguration = new BotConfiguration
            {
                Difficulty = 0,
                UserId = userId.ToString(),
                MatchPlayers = playersList.Select(x => x.UserId.ToString()).ToList(),
                MatchId = null,
                MatchmakerData = playersList[playerIndex].MatchmakerData,
                GameEngineData = playersList[playerIndex].GameEngineData,
            };

            _halfRemoteMatchClient = new HalfRemoteMatchClientAdapter(elympicsGameConfig);
            _halfRemoteMatchConnectClient = new HalfRemoteMatchConnectClient(_halfRemoteMatchClient, elympicsGameConfig, userId);

            _halfRemoteMatchClient.InGameDataUnreliableReceived += gameBotAdapter.OnInGameDataUnreliableReceived;
            gameBotAdapter.InGameDataForReliableChannelGenerated += async data => await _halfRemoteMatchClient.SendRawDataToServer(data, true);
            gameBotAdapter.InGameDataForUnreliableChannelGenerated += async data => await _halfRemoteMatchClient.SendRawDataToServer(data, false);

            gameBotAdapter.Init(null, null);
            gameBotAdapter.Init2(null);
            gameBotAdapter.Init3(botConfiguration);

            _ = _halfRemoteMatchConnectClient.ConnectAndJoinAsPlayer(_ => { }, CancellationToken.None);
        }

        public override void Dispose() => _halfRemoteMatchConnectClient?.Dispose();
    }
}
