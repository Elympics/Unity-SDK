using UnityEngine;

namespace Elympics
{
	internal abstract class GameServerInitializer : GameSceneInitializer
	{
		protected virtual bool HandlingBotsOverride    => false;
		protected virtual bool HandlingClientsOverride => false;

		public override void Initialize(ElympicsClient client, ElympicsBot bot, ElympicsServer server, ElympicsGameConfig elympicsGameConfig)
		{
			Time.fixedDeltaTime = elympicsGameConfig.TickDuration;
			Time.maximumDeltaTime = elympicsGameConfig.TickDuration * 2;
			Application.targetFrameRate = elympicsGameConfig.TicksPerSecond * 2;

			var gameEngine = new GameEngineAdapter(elympicsGameConfig);

			// ElympicsServer has to setup callbacks BEFORE initializing GameEngine - possible loss of events like PlayerConnected or Init ~pprzestrzelski 26.05.2021
			server.InitializeInternal(elympicsGameConfig, gameEngine, HandlingBotsOverride, HandlingClientsOverride);
			InitializeGameServer(elympicsGameConfig, gameEngine);

			client.Destroy();
			bot.Destroy();
		}

		protected abstract void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter);
	}
}