using Elympics.ElympicsSystems;
using Elympics.SnapshotAnalysis;
using UnityConnectors;

namespace Elympics
{
    internal class OnlineGameServerInitializer : GameServerInitializer
    {
        private GameEngineProtoConnector _gameEngineProtoConnector;

        protected override void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter)
        {
            _gameEngineProtoConnector = new GameEngineProtoConnector(gameEngineAdapter);
            _gameEngineProtoConnector.Connect();
        }

        protected override SnapshotAnalysisCollector ProvideSnapSnapshotAnalysisCollector() => new ServerOnlineSnapshotAnalysisCollector(GameEngineAdapter, Server, BehavioursManager);
        protected override IServerPlayerHandler ProvideInputRetriever() => new NullServerPlayerHandler();
        protected override IServerElympicsUpdateLoop ProvideElympicsUpdateLoop() => new DefaultServerElympicsUpdateLoop(BehavioursManager, GameEngineAdapter, Server, GameConfig);

        public override void Dispose()
        {
            base.Dispose();
            _gameEngineProtoConnector?.Dispose();
        }
    }
}
