#nullable enable

using System;
using Elympics.Core.Logger;
using Elympics.Replication;
using UnityEngine;

namespace Elympics
{
    [DefaultExecutionOrder(ElympicsExecutionOrder.GameSceneManager)]
    public class GameSceneManager : MonoBehaviour
    {
        [SerializeField] private ElympicsBehavioursManager elympicsBehavioursManager = null!;
        [SerializeField] private ElympicsClient elympicsClient = null!;
        [SerializeField] private ElympicsServer elympicsServer = null!;

        private GameSceneInitializer? _gameSceneInitializer;
        private bool _disposed;

        public void Awake()
        {
            if (!ApplicationParameters.IsInitialized && !ApplicationParameters.InitializeParameters())
                ExitUtility.ExitGame();

            // Set if the scene was loaded by ElympicsMatchLauncher. Null if Play was pressed directly in this scene.
            var session = ElympicsMatchLauncher.Current?.SessionAwaitingScene;
            if (session != null && !session.TryAttachScene())
                session = null;
            IElympicsMatchLifecycleSink? sink = session;

            try
            {
                var elympicsGameConfig = session?.Launch.GameConfig
                    ?? ElympicsConfig.LoadCurrentElympicsGameConfig()
                    ?? throw new ElympicsException("Game config not found");

                // ElympicsWorld needed for all modes (client uses it for IsVisibleTo bitmask lookup)
                ElympicsWorld.Current = new ElympicsWorld(elympicsGameConfig.MaxPlayers);
                ElympicsLogger.LogInfo($"Initializing Elympics v{ElympicsConfig.SdkVersion} game scene for {elympicsGameConfig.GameName} "
                    + $"(ID: {elympicsGameConfig.GameId}), version {elympicsGameConfig.GameVersion}");
                elympicsBehavioursManager.LifecycleSink = sink;
                _gameSceneInitializer = GameSceneInitializerFactory.Create(elympicsGameConfig, session?.Launch);
                ElympicsLogger.LogInfo($"Created game scene initializer of type {_gameSceneInitializer.GetType().Name}");
                session?.RegisterTeardown("dispose game scene initializer", DisposeInitializer);
                _gameSceneInitializer.Initialize(elympicsClient, elympicsServer, elympicsGameConfig, elympicsBehavioursManager);
                ElympicsLogger.LogInfo("Elympics game scene initialized successfully.");
                if (!_gameSceneInitializer.ReportsMatchJoined)
                    sink?.MatchJoined();
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
                sink?.SceneInitializationFailed(e is ElympicsException ? ElympicsLaunchErrorCode.SceneLoadFailed : ElympicsLaunchErrorCode.InternalError,
                    $"Initializing the gameplay scene failed: {e.GetType().Name}: {e.Message}");
            }
        }

        private void OnDisable() => DisposeInitializer();

        private void DisposeInitializer()
        {
            if (_disposed)
                return;
            _disposed = true;
            _gameSceneInitializer?.Dispose();
        }
    }
}
