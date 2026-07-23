using System;
using Elympics.Core.Logger;
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
                ?? throw ElympicsLogger.LogExceptionAndReturn(new ElympicsException("Game config not found"));

            try
            {
                // ElympicsWorld needed for all modes (client uses it for IsVisibleTo bitmask lookup)
                ElympicsWorld.Current = new ElympicsWorld(elympicsGameConfig!.MaxPlayers);
                ElympicsLogger.LogInfo($"Initializing Elympics v{ElympicsConfig.SdkVersion} game scene for {elympicsGameConfig.GameName} "
                    + $"(ID: {elympicsGameConfig.GameId}), version {elympicsGameConfig.GameVersion}");
                _gameSceneInitializer = GameSceneInitializerFactory.Create(elympicsGameConfig);
                ElympicsLogger.LogInfo($"Created game scene initializer of type {_gameSceneInitializer.GetType().Name}");
                _gameSceneInitializer.Initialize(elympicsClient, elympicsBot, elympicsServer, elympicsGameConfig, elympicsBehavioursManager);
                ElympicsLogger.LogInfo("Elympics game scene initialized successfully.");
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
            }
        }

        private void OnDisable() => _gameSceneInitializer?.Dispose();
    }
}
