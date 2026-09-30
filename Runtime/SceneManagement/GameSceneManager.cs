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

        internal const string AwaitingLaunchMessage = "The gameplay scene has been loaded and awaits a launch method call. To start a match, call one of "
            + "ElympicsMatchLauncher.Instance.Launch*Async methods (e.g. LaunchSinglePlayerAsync), or send a launch command from the host "
            + "(window.elympicsHost.launch* on WebGL, ElympicsHost_Launch* in native builds).";
        internal const string AwaitingLaunchEditorMessage = "The gameplay scene has been loaded and awaits a launch method call. To start a match, use "
            + "Tools > Elympics > Match Launcher, or call one of ElympicsMatchLauncher.Instance.Launch*Async methods (e.g. LaunchSinglePlayerAsync).";

        public void Awake()
        {
            if (!ApplicationParameters.IsInitialized && !ApplicationParameters.InitializeParameters())
                ExitUtility.ExitGame();

            // An online game server runs exactly this scene, whatever the launcher does.
            if (ApplicationParameters.ShouldLoadElympicsOnlineServer)
            {
                var serverConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
                if (serverConfig == null)
                {
                    ElympicsLogger.LogError("Game config not found. Not initializing the online game server.");
                    return;
                }
                Run(serverConfig, null, context =>
                {
                    var initializer = new OnlineGameServerInitializer();
                    context.Adopt(initializer);
                    initializer.Initialize(context.Client, context.Server, context.GameConfig, context.BehavioursManager);
                    return initializer;
                });
                return;
            }

            ElympicsMatchSession? session = null;
            var attachment = ElympicsMatchLauncher.Current?.AttachGameplayScene(out session) ?? ElympicsMatchLauncher.SceneAttachment.NotLauncherDriven;
            switch (attachment)
            {
                case ElympicsMatchLauncher.SceneAttachment.Attached:
                    Run(session!.Launch.GameConfig, session, context => session.Launch.Data.Launch(context));
                    return;
                case ElympicsMatchLauncher.SceneAttachment.Suppressed:
                    ElympicsLogger.LogInfo("Gameplay scene loaded while its match is being torn down. Not initializing it.");
                    return;
                case ElympicsMatchLauncher.SceneAttachment.NotLauncherDriven:
                default:
                    ElympicsLogger.LogInfo(Application.isEditor ? AwaitingLaunchEditorMessage : AwaitingLaunchMessage);
                    return;
            }
        }

        /// <param name="session">The launcher session of the match, or null for an online game server.</param>
        private void Run(ElympicsGameConfig gameConfig, ElympicsMatchSession? session, Func<GameSceneLaunchContext, GameSceneInitializer> launch)
        {
            IElympicsMatchLifecycleSink? sink = session;
            try
            {
                // ElympicsWorld needed for all modes (client uses it for IsVisibleTo bitmask lookup)
                ElympicsWorld.Current = new ElympicsWorld(gameConfig.MaxPlayers);
                ElympicsLogger.LogInfo($"Initializing Elympics v{ElympicsConfig.SdkVersion} game scene for {gameConfig.GameName} "
                    + $"(ID: {gameConfig.GameId}), version {gameConfig.GameVersion}");
                elympicsBehavioursManager.LifecycleSink = sink;
                if (session != null)
                {
                    elympicsClient.MatchToken = session.Token;
                    elympicsServer.MatchToken = session.Token;
                    // The scene keeps ticking until the boot scene replaces it: stop it before its transport is disposed.
                    session.RegisterTeardown("stop simulation", StopSimulation);
                    session.RegisterTeardown("dispose game scene initializer", DisposeInitializer);
                    session.TrackCollectable(elympicsBehavioursManager);
                }

                var context = new GameSceneLaunchContext(elympicsClient, elympicsServer, gameConfig, elympicsBehavioursManager, session?.SnapshotRetriever, initializer =>
                {
                    _gameSceneInitializer = initializer;
                    initializer.LifecycleSink = sink;
                    ElympicsLogger.LogInfo($"Created game scene initializer of type {initializer.GetType().Name}");
                });
                var initialized = launch(context);
                ElympicsLogger.LogInfo("Elympics game scene initialized successfully.");
                if (!initialized.ReportsMatchJoined)
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
