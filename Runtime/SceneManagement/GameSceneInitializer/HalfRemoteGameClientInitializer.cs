using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Communication.Models.Public;
using Elympics.ElympicsSystems.Internal;
using Elympics.Mappers;
namespace Elympics
{
    internal class HalfRemoteGameClientInitializer : GameClientInitializer
    {
        private HalfRemoteMatchClientAdapter _halfRemoteMatchClient;
        private HalfRemoteMatchConnectClient _halfRemoteMatchConnectClient;

        protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
        {
            const string gameModeName = "half remote";
            var playerIndex = elympicsGameConfig.PlayerIndexForHalfRemoteMode;
            var queueName = elympicsGameConfig.TestMatchData.queueName;
            var regionName = elympicsGameConfig.TestMatchData.regionName;
            var customRoomData = elympicsGameConfig.TestMatchData.roomCustomData;
            var customMatchmakingData = elympicsGameConfig.TestMatchData.customMatchmakingData;

            var playersList = DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig);

            if (playersList.Count <= playerIndex)
                throw ElympicsLogger.LogException("Half Remote client won't be initialized because "
                    + $"no data for player ID: {playerIndex} was found in \"Test players\" list. "
                    + $"The list has only {playersList.Count} entries. "
                    + $"Try increasing \"Players\" count in your {nameof(ElympicsGameConfig)}.");
            var logger = ElympicsLogger.CurrentContext ?? new ElympicsLoggerContext(Guid.NewGuid());
            logger = logger.SetGameMode(gameModeName).WithApp(ElympicsLoggerContext.GameplayContextApp).SetElympicsContext(ElympicsConfig.SdkVersion, elympicsGameConfig.gameId);
            var userId = playersList[playerIndex].UserId;
            var matchmakerData = playersList[playerIndex].MatchmakerData;
            var gameEngineData = playersList[playerIndex].GameEngineData;

            _halfRemoteMatchClient = new HalfRemoteMatchClientAdapter(elympicsGameConfig);
            var halfRemoteMatchInitialData = new MatchInitialData
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
                CustomMatchmakingData = customMatchmakingData.ToDictionary(x => x.key,x=> x.value),
                ExternalGameData = new byte[] { },
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
            _halfRemoteMatchConnectClient = new HalfRemoteMatchConnectClient(_halfRemoteMatchClient, elympicsGameConfig, userId, halfRemoteMatchInitialData);
            client.InitializeInternal(elympicsGameConfig,
                _halfRemoteMatchConnectClient,
                _halfRemoteMatchClient,
                new InitialMatchPlayerDataGuid(ElympicsPlayer.FromIndex(playerIndex), gameEngineData, matchmakerData)
                {
                    UserId = userId,
                    IsBot = false,
                },
                ElympicsBehavioursManager,
                logger);
        }

        public override void Dispose() => _halfRemoteMatchConnectClient?.Dispose();
    }
}
