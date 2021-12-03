
using UnityEngine;

namespace Elympics
{
	internal abstract class GameBotInitializer : GameSceneInitializer
	{
		public override void Initialize(ElympicsClient client, ElympicsBot bot, ElympicsServer server, ElympicsGameConfig elympicsGameConfig)
		{
			Application.targetFrameRate = elympicsGameConfig.TicksPerSecond;

			var gameBotAdapter = new GameBotAdapter();

			// ElympicsBot has to setup callbacks BEFORE initializing GameBotAdapter - possible loss of events like Init ~pprzestrzelski 27.08.2021
			bot.InitializeInternal(elympicsGameConfig, gameBotAdapter);
			InitializeBot(bot, elympicsGameConfig, gameBotAdapter);
			
			client.Destroy();
			server.Destroy();
		}

		protected abstract void InitializeBot(ElympicsBot bot, ElympicsGameConfig elympicsGameConfig, GameBotAdapter gameBotAdapter);
	}
}
