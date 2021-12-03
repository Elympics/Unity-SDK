﻿using UnityEngine;

namespace Elympics
{
	internal abstract class GameServerInitializer : GameSceneInitializer
	{
		public override void Initialize(ElympicsClient client, ElympicsBot bot, ElympicsServer server, ElympicsGameConfig elympicsGameConfig)
		{
			Application.targetFrameRate = elympicsGameConfig.TicksPerSecond;

			var gameEngine = new GameEngineAdapter(elympicsGameConfig);

			// ElympicsServer has to setup callbacks BEFORE initializing GameEngine - possible loss of events like PlayerConnected or Init ~pprzestrzelski 26.05.2021
			server.InitializeInternal(elympicsGameConfig, gameEngine);
			InitializeGameServer(elympicsGameConfig, gameEngine);

			client.Destroy();
			bot.Destroy();
		}

		protected abstract void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter);
	}
}
