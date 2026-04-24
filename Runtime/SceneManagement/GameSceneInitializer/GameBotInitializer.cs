using UnityEngine;

namespace Elympics
{
    internal abstract class GameBotInitializer : GameSceneInitializer
    {
        public override void Initialize(
            ElympicsClient client,
            ElympicsBot bot,
            ElympicsServer server,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager)
        {
            Time.maximumDeltaTime = gameConfig.TickDuration * 2;
            Application.targetFrameRate = gameConfig.TicksPerSecond * 2;

            var gameBotAdapter = new GameBotAdapter();

            // ElympicsBot has to setup callbacks BEFORE initializing GameBotAdapter - possible loss of events like Init ~pprzestrzelski 27.08.2021
            bot.InitializeInternal(gameConfig, gameBotAdapter, behavioursManager);
            InitializeBot(bot, gameConfig, gameBotAdapter);
            behavioursManager.InitializeInternal(bot, gameConfig.MaxPlayers);

            client.Destroy();
            server.Destroy();
            // singlePlayer.Destroy();
        }

        protected abstract void InitializeBot(ElympicsBot bot, ElympicsGameConfig elympicsGameConfig, GameBotAdapter gameBotAdapter);
    }
}
