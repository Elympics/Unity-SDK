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

            var logger = new ElympicsLoggerContext(Guid.NewGuid())
                .SetElympicsContext(ElympicsConfig.SdkVersion, elympicsGameConfig.gameId)
                .WithApp(ElympicsLoggerContext.GameplayContextApp);
            ElympicsLogger.CurrentContext = logger;

            try
            {
                // ElympicsWorld needed for all modes (client uses it for IsVisibleTo bitmask lookup)
                ElympicsWorld.Current = new ElympicsWorld(elympicsGameConfig!.MaxPlayers);
                logger.Log($"Initializing Elympics v{ElympicsConfig.SdkVersion} game scene for {elympicsGameConfig.GameName} "
                    + $"(ID: {elympicsGameConfig.GameId}), version {elympicsGameConfig.GameVersion}");
                _gameSceneInitializer = GameSceneInitializerFactory.Create(elympicsGameConfig);
                logger.Log($"Created game scene initializer of type {_gameSceneInitializer.GetType().Name}");
                _gameSceneInitializer.Initialize(elympicsClient, elympicsBot, elympicsServer, elympicsGameConfig, elympicsBehavioursManager, logger);
                logger.Log("Elympics game scene initialized successfully.");
            }
            catch (Exception e)
            {
                logger.Exception(e);
            }
        }

        private void OnDisable() => _gameSceneInitializer?.Dispose();
    }
}
