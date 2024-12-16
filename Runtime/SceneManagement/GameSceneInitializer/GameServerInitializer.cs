using UnityEngine;

namespace Elympics
{
    internal abstract class GameServerInitializer : GameSceneInitializer
    {
        protected virtual bool HandlingBotsOverride => false;
        protected virtual bool HandlingClientsOverride => false;

        public override void Initialize(
            ElympicsClient client,
            ElympicsBot bot,
            ElympicsServer server,
            ElympicsSinglePlayer singlePlayer,
            ElympicsGameConfig config,
            ElympicsBehavioursManager behavioursManager)
        {
            Time.maximumDeltaTime = config.TickDuration * 2;
            Application.targetFrameRate = config.TicksPerSecond * 2;

            var gameEngine = new GameEngineAdapter(config);

            // ElympicsServer has to setup callbacks BEFORE initializing GameEngine - possible loss of events like PlayerConnected or Init ~pprzestrzelski 26.05.2021
            server.InitializeInternal(config, gameEngine, behavioursManager, HandlingBotsOverride, HandlingClientsOverride);
            InitializeGameServer(config, gameEngine);
            behavioursManager.InitializeInternal(server);

            client.Destroy();
            bot.Destroy();
            // singlePlayer.Destroy();
        }

        protected abstract void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter);
    }
}
