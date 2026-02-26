using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Elympics.ElympicsSystems;
using Elympics.SnapshotAnalysis;
using Elympics.SnapshotAnalysis.Retrievers;
using GameEngineCore.V1._4;

namespace Elympics
{
    internal abstract class SnapshotReplayInitializer : GameSceneInitializer
    {
        protected void InitializeInternal(
            ElympicsBot bot,
            ElympicsServer server,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager,
            SnapshotAnalysisRetriever snapshotRetriever,
            IReplayManipulator replayManipulator)
        {
            // ElympicsServer has to setup callbacks BEFORE initializing GameEngine - possible loss of events like PlayerConnected or Init ~pprzestrzelski 26.05.2021
            var gameEngineAdapter = new GameEngineAdapter(gameConfig);
            var initData = snapshotRetriever.RetrieveInitData();
            //TODO Right now we drop support for bots on singleplayer. ~kpieta 20.02.2025
            var snapshots = snapshotRetriever.RetrieveSnapshots();
            var updateLoop = new SnapshotReplayElympicsUpdateLoop(behavioursManager, snapshots, replayManipulator);
            server.InitializeInternal(gameConfig,
                gameEngineAdapter,
                behavioursManager,
                new NullServerPlayerHandler(),
                new NullSnapshotAnalysisCollector(),
                updateLoop,
                true);

            // @formatter:off
            gameEngineAdapter.Initialize(new InitialMatchData
            {
                UserData = initData.PlayerData.Select(x =>
                {
                    var toReturn = new InitialMatchUserData
                    {
                        UserId = x.UserId,
                        IsBot = x.IsBot,
                        BotDifficulty = x.BotDifficulty,
                        MatchmakerData = x.MatchmakerData,
                        GameEngineData = x.GameEngineData,
                        RoomId = x.RoomId,
                        TeamIndex = x.TeamIndex
                    };
                    return toReturn;
                }).ToList(),
                MatchId = initData.CollectorMatchData.MatchId,
                QueueName = initData.CollectorMatchData.QueueName,
                RegionName = initData.CollectorMatchData.RegionName,
                CustomRoomData = initData.CollectorMatchData.CustomRoomData == null ? null : new ReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>>(
                    initData.CollectorMatchData.CustomRoomData.ToDictionary(x => x.Key, x => (IReadOnlyDictionary<string, string>)new ReadOnlyDictionary<string, string>(x.Value))),
                CustomMatchmakingData = initData.CollectorMatchData.CustomMatchmakingData?.ToDictionary(x => x.Key, x => x.Value),
                ExternalGameData = initData.ExternalGameData
            },
            true);
            // @formatter:on
            behavioursManager.InitializeInternal(server, gameConfig.MaxPlayers);
            bot.Destroy();

            replayManipulator.LoadReplay(updateLoop, new ReplayData { InitData = initData, Snapshots = snapshots });
        }
    }
}
