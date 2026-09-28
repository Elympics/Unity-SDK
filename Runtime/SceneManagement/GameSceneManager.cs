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
            ElympicsMatchSession? session = null;
            var attachment = ElympicsMatchLauncher.Current?.AttachGameplayScene(out session) ?? ElympicsMatchLauncher.SceneAttachment.NotLauncherDriven;
            if (attachment == ElympicsMatchLauncher.SceneAttachment.Suppressed)
            {
                ElympicsLogger.LogInfo("Gameplay scene loaded while its match is being torn down. Not initializing it.");
                return;
            }
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
                _gameSceneInitializer.LifecycleSink = sink;
                if (session != null)
                {
                    elympicsClient.MatchToken = session.Token;
                    elympicsServer.MatchToken = session.Token;
                }
                if (session != null)
                {
                    // The scene keeps ticking until the boot scene replaces it: stop it before its transport is disposed.
                    session.RegisterTeardown("stop simulation", StopSimulation);
                    session.RegisterTeardown("dispose game scene initializer", DisposeInitializer);
                    session.TrackCollectable(elympicsBehavioursManager);
                }
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

        private void StopSimulation()
        {
            if (elympicsClient != null)
                elympicsClient.enabled = false;
            if (elympicsServer != null)
                elympicsServer.enabled = false;
        }

        private void DisposeInitializer()
        {
            if (_disposed)
                return;
            _disposed = true;
            _gameSceneInitializer?.Dispose();
        }
    }
}
