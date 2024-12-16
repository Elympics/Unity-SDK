using UnityEngine;

namespace Elympics
{
    internal abstract class GameClientInitializer : GameSceneInitializer
    {
        protected ElympicsBehavioursManager ElympicsBehavioursManager { get; private set; }
        private ElympicsClient _client;

        public override void Initialize(
            ElympicsClient client,
            ElympicsBot bot,
            ElympicsServer server,
            ElympicsSinglePlayer singlePlayer,
            ElympicsGameConfig config,
            ElympicsBehavioursManager behavioursManager)
        {
            ElympicsBehavioursManager = behavioursManager;
            Time.fixedDeltaTime = config.TickDuration;

            _client = client;
            bot.Destroy();
            server.Destroy();
            // singlePlayer.Destroy();
            InitializeClient(client, config);
            behavioursManager.InitializeInternal(client);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_client != null && _client.Initialized)
                _client.MatchConnectClient.Dispose();
        }

        protected abstract void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig);
    }
}
