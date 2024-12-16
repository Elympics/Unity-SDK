using System.Collections.Generic;
using Elympics.ElympicsSystems.Internal;
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
            ElympicsGameConfig config,
            ElympicsBehavioursManager behavioursManager)
        {
            // ElympicsServer has to setup callbacks BEFORE initializing GameEngine - possible loss of events like PlayerConnected or Init ~pprzestrzelski 26.05.2021
            var gameEngineAdapter = new GameEngineAdapter(config);
            var gameLogger = ElympicsLogger.CurrentContext!.Value.WithApp(ElympicsLoggerContext.GameplayContextApp);
            var elConfig = ElympicsConfig.Load();
            _gameEngine = new SinglePlayerGameEngine(gameEngineAdapter, elConfig, gameLogger);
            //TODO Right now we drop support for bots on singleplayer. ~kpieta 20.02.2025
            server.InitializeInternal(config, gameEngineAdapter, behavioursManager, false, true);
            var matchData = ElympicsLobbyClient.Instance!.MatchDataGuid ?? throw new ElympicsException("Couldn't find matchData.");


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
                ExternalGameData = matchData.GameEngineData
            });

            behavioursManager.InitializeInternal(server);
            bot.Destroy();
        }

        public override void Dispose() => _gameEngine.Dispose();
    }
}
