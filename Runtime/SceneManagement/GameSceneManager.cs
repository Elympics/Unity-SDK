using System;
using UnityEngine;

namespace Elympics
{
    public class GameSceneManager : MonoBehaviour
    {
        [SerializeField] private ElympicsClient elympicsClient;
        [SerializeField] private ElympicsBot elympicsBot;
        [SerializeField] private ElympicsServer elympicsServer;

        private GameSceneInitializer _gameSceneInitializer;

        public void Awake()
        {
            try
            {
                var elympicsGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
                ElympicsLogger.Log($"Initializing Elympics v{ElympicsConfig.SdkVersion} game scene for {elympicsGameConfig.GameName} "
                    + $"(ID: {elympicsGameConfig.GameId}), version {elympicsGameConfig.GameVersion}");
                _gameSceneInitializer = GameSceneInitializerFactory.Create(elympicsGameConfig);
                ElympicsLogger.Log($"Created game scene initializer of type {_gameSceneInitializer.GetType().Name}");
                _gameSceneInitializer.Initialize(elympicsClient, elympicsBot, elympicsServer, elympicsGameConfig);
                ElympicsLogger.Log("Elympics game scene initialized successfully.");
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException(e);
            }
        }

        private void OnDisable()
        {
            _gameSceneInitializer?.Dispose();
        }
    }
}
