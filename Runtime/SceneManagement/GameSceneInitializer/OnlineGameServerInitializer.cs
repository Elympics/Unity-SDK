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

        public override void Dispose()
        {
            base.Dispose();
            _gameEngineProtoConnector?.Dispose();
        }
    }
}
