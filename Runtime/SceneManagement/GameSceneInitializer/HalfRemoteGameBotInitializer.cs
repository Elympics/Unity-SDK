using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elympics.Communication.Models.Public;
using Elympics.Mappers;
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
            var queueName = elympicsGameConfig.TestMatchData.queueName;
            var regionName = elympicsGameConfig.TestMatchData.regionName;
            var customRoomData = elympicsGameConfig.TestMatchData.roomCustomData;
            var customMatchmakingData = elympicsGameConfig.TestMatchData.customMatchmakingData;

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
            var matchInitData = new MatchInitialData
            {
                MatchId = Guid.Empty,
                IsReplay = false,
                QueueName = queueName,
                RegionName = regionName,
                CustomRoomData = customRoomData.Select((x, index) =>
                {
                    var roomId = new Guid(index, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                    var roomCustomData = x.roomCustomData.ToDictionary(customData => customData.key, customData => customData.value);
                    return (roomid: roomId, (IReadOnlyDictionary<string, string>)roomCustomData);

                }).ToDictionary(pair => pair.roomid, pair => pair.Item2),
                CustomMatchmakingData = customMatchmakingData.ToDictionary(x => x.key, x => x.value),
                ExternalGameData = new byte[]
                    { },
                PlayerInitialDatas = playersList.Select((x, index) => new PlayerInitialData
                {
                    Player = ElympicsPlayer.FromIndex(index),
                    UserId = x.UserId,
                    IsBot = x.IsBot,
                    BotDifficulty = x.BotDifficulty,
                    GameEngineData = x.GameEngineData,
                    MatchmakerData = x.MatchmakerData,
                    RoomId = x.RoomId,
                    TeamIndex = x.TeamIndex,
                    Nickname = x.Nickname,
                    NicknameType = NicknameMapper.ConvertToNickNameType(x.NicknameType),
                    CustomData = x.CustomData
                }).ToList()
            };
            _halfRemoteMatchConnectClient = new HalfRemoteMatchConnectClient(_halfRemoteMatchClient, elympicsGameConfig, userId, matchInitData);

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
