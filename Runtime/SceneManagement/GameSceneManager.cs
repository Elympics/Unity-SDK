using System;
using Elympics.Core.Logger;
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

            var elympicsConfig = ElympicsConfig.Load();
            var elympicsGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig()
                ?? throw ElympicsLogger.LogExceptionAndReturn(new ElympicsException("Game config not found"));

            _ = ElympicsLogger.ApplicationState.SetSdkConfiguration(ElympicsConfig.SdkVersion, elympicsConfig.ElympicsApiEndpoint, elympicsConfig.ElympicsGameServersEndpoint)
                // TODO: .SetGameId(elympicsGameConfig.gameId)
                ;
            var logger = ElympicsLogger.Config
                .WithElympicsGameService();

            try
            {
                // ElympicsWorld needed for all modes (client uses it for IsVisibleTo bitmask lookup)
                ElympicsWorld.Current = new ElympicsWorld(elympicsGameConfig!.MaxPlayers);
                logger.LogInfo($"Initializing Elympics v{ElympicsConfig.SdkVersion} game scene for {elympicsGameConfig.GameName} "
                    + $"(ID: {elympicsGameConfig.GameId}), version {elympicsGameConfig.GameVersion}");
                _gameSceneInitializer = GameSceneInitializerFactory.Create(elympicsGameConfig);
                logger.LogInfo($"Created game scene initializer of type {_gameSceneInitializer.GetType().Name}");
                _gameSceneInitializer.Initialize(elympicsClient, elympicsBot, elympicsServer, elympicsGameConfig, elympicsBehavioursManager);
                logger.LogInfo("Elympics game scene initialized successfully.");
            }
            catch (Exception e)
            {
                logger.LogException(e);
            }
        }

        private void OnDisable() => _gameSceneInitializer?.Dispose();
    }
}
