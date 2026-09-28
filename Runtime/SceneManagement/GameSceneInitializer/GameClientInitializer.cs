using UnityEngine;

namespace Elympics
{
    internal abstract class GameClientInitializer : GameSceneInitializer
    {
        protected ElympicsBehavioursManager ElympicsBehavioursManager { get; private set; }
        private ElympicsClient _client;

        public override void Initialize(
            ElympicsClient client,
            ElympicsServer server,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager)
        {
            ElympicsBehavioursManager = behavioursManager;
            Time.fixedDeltaTime = gameConfig.TickDuration;

            _client = client;
            server.Destroy();
            // singlePlayer.Destroy();
            //behavioursManager.InitializeInternal(client); po pierwsze to powinno byc PRZED inicjalizacja klienta. Po drugie flow inicjalizacji behMenagera jest strasznie zalezne od ElympicsBase zamiast od argumentow.
            InitializeClient(client, gameConfig);
        }

        public override void Dispose()
        {
            base.Dispose();
            // Not guarded by Initialized: a launch that failed to connect still holds a socket or peer connection.
            // `?.` on purpose: the managed connection must be disposed even if Unity already destroyed the component.
            _client?.DisposeMatchConnectClient();
        }

        protected abstract void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig);
    }
}
