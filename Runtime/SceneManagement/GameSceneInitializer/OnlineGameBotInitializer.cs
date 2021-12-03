using UnityConnectors;
using UnityEngine;

namespace Elympics
{
	internal class OnlineGameBotInitializer : GameBotInitializer
	{
		private GameBotProtoConnector _gameBotProtoConnector;

		protected override void InitializeBot(ElympicsBot bot, ElympicsGameConfig elympicsGameConfig, GameBotAdapter gameBotAdapter)
		{
			_gameBotProtoConnector = new GameBotProtoConnector(gameBotAdapter);
			_gameBotProtoConnector.Connect();
		}

		public override void Dispose()
		{
			base.Dispose();
			_gameBotProtoConnector?.Dispose();
		}
	}
}
