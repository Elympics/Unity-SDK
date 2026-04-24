using System;
using Elympics.ElympicsSystems.Internal;
using Elympics.Replication;
using UnityEngine;

namespace Elympics
{
    [DefaultExecutionOrder(ElympicsExecutionOrder.GameSceneManager)]
    public class GameSceneManager : MonoBehaviour
    {
        [SerializeField] private ElympicsBehavioursManager elympicsBehavioursManager;
        [SerializeField] private ElympicsClient elympicsClient;
        [SerializeField] private ElympicsBot elympicsBot;
        [SerializeField] private ElympicsServer elympicsServer;

        private GameSceneInitializer _gameSceneInitializer;

        public void Awake()
        {
            if (!ApplicationParameters.InitializeParameters())
                ExitUtility.ExitGame();

            var elympicsGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig()
                ?? throw ElympicsLogger.LogException(new ElympicsException("Game config not found"));

            ElympicsLogger.CurrentContext = ElympicsLogger.CurrentContext
                .WithApp(ElympicsLoggerContext.GameplayContextApp)
                .SetElympicsContext(ElympicsConfig.SdkVersion, elympicsGameConfig.gameId);

            try
            {
                // ElympicsWorld needed for all modes (client uses it for IsVisibleTo bitmask lookup)
                ElympicsWorld.Current = new ElympicsWorld(elympicsGameConfig!.MaxPlayers);
                ElympicsLogger.Log($"Initializing Elympics v{ElympicsConfig.SdkVersion} game scene for {elympicsGameConfig.GameName} "
                    + $"(ID: {elympicsGameConfig.GameId}), version {elympicsGameConfig.GameVersion}");
                _gameSceneInitializer = GameSceneInitializerFactory.Create(elympicsGameConfig);
                ElympicsLogger.Log($"Created game scene initializer of type {_gameSceneInitializer.GetType().Name}");
                _gameSceneInitializer.Initialize(elympicsClient, elympicsBot, elympicsServer, elympicsGameConfig, elympicsBehavioursManager);
                ElympicsLogger.Log("Elympics game scene initialized successfully.");
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException(e);
            }
        }

        private void OnDisable() => _gameSceneInitializer?.Dispose();
    }
}
