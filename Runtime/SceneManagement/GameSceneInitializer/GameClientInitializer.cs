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
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager)
        {
            ElympicsBehavioursManager = behavioursManager;
            Time.fixedDeltaTime = gameConfig.TickDuration;

            _client = client;
            bot.Destroy();
            server.Destroy();
            // singlePlayer.Destroy();
            //behavioursManager.InitializeInternal(client); po pierwsze to powinno byc PRZED inicjalizacja klienta. Po drugie flow inicjalizacji behMenagera jest strasznie zalezne od ElympcisBase zamiast od argumentow.
            InitializeClient(client, gameConfig);
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
