using System;
using Elympics.ElympicsSystems;
using Elympics.ElympicsSystems.Internal;
using Elympics.Models.Matchmaking;
using Elympics.SnapshotAnalysis;
using GameEngineCore.V1._4;

namespace Elympics
{
    internal class SinglePlayerGameInitializer : GameSceneInitializer
    {
        private SinglePlayerGameEngine _gameEngine;
        public override void Initialize(
            ElympicsClient client,
            ElympicsBot bot,
            ElympicsServer server,
            ElympicsSinglePlayer singlePlayer,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager)
        {
            var matchData = ElympicsLobbyClient.Instance?.MatchDataGuid ?? new MatchmakingFinishedData(Guid.Empty, string.Empty, string.Empty, string.Empty, Array.Empty<byte>(), Array.Empty<float>(), string.Empty, string.Empty, new[] { Guid.Empty });

            // ElympicsServer has to setup callbacks BEFORE initializing GameEngine - possible loss of events like PlayerConnected or Init ~pprzestrzelski 26.05.2021
            var gameEngineAdapter = new GameEngineAdapter(gameConfig);
            var config = ElympicsConfig.Load() ?? throw new Exception("Missing ElympicsConfig");
            var loggerContext = new ElympicsLoggerContext(Guid.Empty).SetElympicsContext(ElympicsConfig.SdkVersion, config.GetCurrentGameConfig().gameId).SetGameMode("single player").WithApp(ElympicsLoggerContext.GameplayContextApp);
            _gameEngine = new SinglePlayerGameEngine(gameEngineAdapter, config, loggerContext, behavioursManager, matchData.MatchId);
            //TODO Right now we drop support for bots on singleplayer. ~kpieta 20.02.2025
            server.InitializeInternal(gameConfig,
                gameEngineAdapter,
                behavioursManager,
                new SinglePlayerPlayerHandler(server, gameEngineAdapter, behavioursManager),
                new SinglePlayerSnapshotAnalysisCollector(),
                new DefaultServerElympicsUpdateLoop(behavioursManager, gameEngineAdapter, server, gameConfig),
                handlingClientsOverride: true);

            gameEngineAdapter.Initialize(new InitialMatchData
            {
                UserData = new InitialMatchUserData[]
                {
                    new()
                    {
                        UserId = matchData.MatchedPlayers[0],
                        IsBot = false,
                        BotDifficulty = 0,
                        MatchmakerData = matchData.MatchmakerData,
                        GameEngineData = matchData.GameEngineData,
                        RoomId = null, //TODO: can be added when full list of users will be delivered to client. ~k.pieta 21.02.2025
                        TeamIndex = 0
                    }
                },
                MatchId = matchData.MatchId,
                QueueName = matchData.QueueName,
                RegionName = matchData.RegionName,
                CustomRoomData = null, //TODO: can be added when full list of users will be delivered to client. ~k.pieta 21.02.2025
                CustomMatchmakingData = null, //TODO: can be added when full list of users will be delivered to client. ~k.pieta 21.02.2025
                // ExternalGameData = matchData.GameEngineData
                ExternalGameData = Array.Empty<byte>()
            });

            behavioursManager.InitializeInternal(server);
            bot.Destroy();
        }

        public override void Dispose() => _gameEngine.Dispose();
    }
}
