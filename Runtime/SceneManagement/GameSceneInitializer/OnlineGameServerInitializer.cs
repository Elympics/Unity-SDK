using System;
using UnityConnectors;

namespace Elympics
{
	internal class OnlineGameServerInitializer : GameServerInitializer
	{
		private GameEngineAdapter _gameEngineAdapter;
		private GameEngineProtoConnector _gameEngineProtoConnector;

		protected override void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter)
		{
			_gameEngineAdapter = gameEngineAdapter;
			_gameEngineProtoConnector = new GameEngineProtoConnector(gameEngineAdapter);
			_gameEngineAdapter.Initialized += SendInitialized;
			_gameEngineProtoConnector.Connect();
		}

		public override void Dispose()
		{
			base.Dispose();
			_gameEngineProtoConnector?.Dispose();
		}

		private void SendInitialized()
		{
			_gameEngineAdapter.Initialized -= SendInitialized;
			_gameEngineProtoConnector.SendInitialized();
		}
	}
}
