using Elympics.ElympicsSystems;
using Elympics.SnapshotAnalysis;
using UnityEngine;

namespace Elympics
{
    internal abstract class GameServerInitializer : GameSceneInitializer
    {
        protected virtual bool HandlingBotsOverride => false;
        protected virtual bool HandlingClientsOverride => false;

        protected GameEngineAdapter GameEngineAdapter;
        protected ElympicsBehavioursManager BehavioursManager;
        protected ElympicsServer Server;
        protected ElympicsGameConfig GameConfig;

        public override void Initialize(
            ElympicsClient client,
            ElympicsBot bot,
            ElympicsServer server,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager)
        {
            Time.maximumDeltaTime = gameConfig.TickDuration * 2;
            Application.targetFrameRate = gameConfig.TicksPerSecond * 2;

            GameEngineAdapter = new GameEngineAdapter(gameConfig);
            BehavioursManager = behavioursManager;
            Server = server;
            GameConfig = gameConfig;
            // ElympicsServer has to setup callbacks BEFORE initializing GameEngine - possible loss of events like PlayerConnected or Init ~pprzestrzelski 26.05.2021
            Server.InitializeInternal(GameConfig, GameEngineAdapter, BehavioursManager, ProvideInputRetriever(), ProvideSnapSnapshotAnalysisCollector(), ProvideElympicsUpdateLoop(), HandlingBotsOverride, HandlingClientsOverride);
            InitializeGameServer(GameConfig, GameEngineAdapter);
            BehavioursManager.InitializeInternal(Server, GameConfig.MaxPlayers);

            client.Destroy();
            bot.Destroy();
            // singlePlayer.Destroy();
        }

        protected abstract void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter);

        protected abstract SnapshotAnalysisCollector ProvideSnapSnapshotAnalysisCollector();
        protected abstract IServerPlayerHandler ProvideInputRetriever();

        protected abstract IServerElympicsUpdateLoop ProvideElympicsUpdateLoop();
    }
}
