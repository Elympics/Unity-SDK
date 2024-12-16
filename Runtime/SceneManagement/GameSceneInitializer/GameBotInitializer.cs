using UnityEngine;

namespace Elympics
{
    internal abstract class GameBotInitializer : GameSceneInitializer
    {
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

            var gameBotAdapter = new GameBotAdapter();

            // ElympicsBot has to setup callbacks BEFORE initializing GameBotAdapter - possible loss of events like Init ~pprzestrzelski 27.08.2021
            bot.InitializeInternal(config, gameBotAdapter, behavioursManager);
            InitializeBot(bot, config, gameBotAdapter);
            behavioursManager.InitializeInternal(bot);

            client.Destroy();
            server.Destroy();
            // singlePlayer.Destroy();
        }

        protected abstract void InitializeBot(ElympicsBot bot, ElympicsGameConfig elympicsGameConfig, GameBotAdapter gameBotAdapter);
    }
}
