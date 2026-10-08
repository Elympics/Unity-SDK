#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Core.Logger;
using Elympics.Replication;
using Elympics.SnapshotAnalysis.Retrievers;
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

        /// <summary>What initializes the gameplay scene, in order of priority.</summary>
        internal enum Driver
        {
            OnlineServer,
            /// <summary>The <c>ELYMPICS_LAUNCH_MODE</c> parameter, which limits the process to one match.</summary>
            LaunchModeParameter,
            Session,
            /// <summary>The scene activated while its match is being torn down.</summary>
            Suppressed,
            None,
        }

        internal static Driver ChooseDriver(bool isOnlineServer, bool isLaunchModeSet, Func<DefaultElympicsMatchLauncher.SceneAttachment> attach)
        {
            if (isOnlineServer)
                return Driver.OnlineServer;
            if (isLaunchModeSet)
                return Driver.LaunchModeParameter;
            return attach() switch
            {
                DefaultElympicsMatchLauncher.SceneAttachment.Attached => Driver.Session,
                DefaultElympicsMatchLauncher.SceneAttachment.Suppressed => Driver.Suppressed,
                _ => Driver.None,
            };
        }

        // Everything up to choosing the driver runs synchronously, keeping the order against other scene Awakes.
        public void Awake() => AwakeAsync().Forget();

        private async UniTaskVoid AwakeAsync()
        {
            if (!ApplicationParameters.IsInitialized && !ApplicationParameters.InitializeParameters())
                ExitUtility.ExitGame();

            ElympicsMatchSession? session = null;
            var driver = ChooseDriver(ApplicationParameters.ShouldLoadElympicsOnlineServer, LaunchDataFromParameters.IsSet,
                () => ElympicsMatchLauncher.Current?.AttachGameplayScene(out session) ?? DefaultElympicsMatchLauncher.SceneAttachment.NotLauncherDriven);
            switch (driver)
            {
                case Driver.OnlineServer:
#if UNITY_WEBGL && !UNITY_EDITOR
                    // Left out of WebGL players, so the linker drops the game-engine connector and protobuf.
                    ElympicsLogger.LogError("The online server can't run in a WebGL player.");
#else
                    if (LoadGameConfig() is { } serverConfig)
                        Run(serverConfig, null, null, null, context => context.Start(new OnlineGameServerInitializer()));
#endif
                    return;
                case Driver.LaunchModeParameter:
                    await RunFromLaunchModeParameterAsync();
                    return;
                case Driver.Session:
                    Run(session!.Launch.GameConfig, session, session, session.SnapshotRetriever, context => session.Launch.Data.Launch(context));
                    return;
                case Driver.Suppressed:
                    ElympicsLogger.LogInfo("Gameplay scene loaded while its match is being torn down. Not initializing it.");
                    return;
                case Driver.None:
                default:
                    ElympicsLogger.LogInfo(Application.isEditor ? AwaitingLaunchEditorMessage : AwaitingLaunchMessage);
                    return;
            }
        }

        private async UniTask RunFromLaunchModeParameterAsync()
        {
            var sink = new ExitAfterMatchSink(_ => ExitUtility.ExitGame());
            try
            {
                var parameters = ApplicationParameters.Parameters;
                var mode = parameters.LaunchMode.GetValue();
                var gameConfig = LoadGameConfig();
                if (gameConfig == null)
                {
                    sink.SceneInitializationFailed(ElympicsLaunchErrorCode.UnknownGame, "Game config not found.");
                    return;
                }
                if (!LaunchDataFromParameters.TryCreate(mode, parameters.ReplayPath.GetValue(), gameConfig, out var data, out var error))
                {
                    sink.SceneInitializationFailed(ElympicsLaunchErrorCode.UnsupportedMode, error);
                    return;
                }
                if (!data!.TryValidate(gameConfig, ElympicsLaunchPlatform.Current, out var failure))
                {
                    sink.SceneInitializationFailed(failure!.Code, failure.message);
                    return;
                }
                ElympicsLogger.LogInfo($"Starting a {data.ModeName} match from the launch mode parameter.");

                SnapshotAnalysisRetriever? retriever = null;
                if (data is SnapshotReplayLaunchData replay)
                {
                    var destroyed = this.GetCancellationTokenOnDestroy();
                    var result = await SnapshotReplayLoader.LoadAsync(replay, gameConfig, destroyed);
                    if (!IsStillWanted(destroyed))
                        return;
                    if (result.Failure != null)
                    {
                        sink.SceneInitializationFailed(result.Failure.Code, result.Failure.message);
                        return;
                    }
                    retriever = result.Retriever;
                }
                Run(gameConfig, null, sink, retriever, data.Launch);
            }
            catch (OperationCanceledException)
            {
                // The scene was destroyed while the replay was downloading.
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
                sink.SceneInitializationFailed(ElympicsLaunchErrorCode.InternalError, $"{e.GetType().Name}: {e.Message}");
            }
        }

        /// <summary>After an await: false if the scene was destroyed (or disabled, so its initializer is already disposed) meanwhile.</summary>
        internal bool IsStillWanted(CancellationToken destroyed) => !destroyed.IsCancellationRequested && !_disposed;

        private static ElympicsGameConfig? LoadGameConfig()
        {
            var gameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
            if (gameConfig == null)
                ElympicsLogger.LogError("Game config not found. Not initializing the gameplay scene.");
            return gameConfig;
        }

        private void Run(ElympicsGameConfig gameConfig, ElympicsMatchSession? session, IElympicsMatchLifecycleSink? sink, SnapshotAnalysisRetriever? snapshotRetriever,
            Func<GameSceneLaunchContext, GameSceneInitializer> launch)
        {
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
                    session.TrackCollectable(elympicsClient);
                    session.TrackCollectable(elympicsServer);
                    session.TrackCollectable(this);
                }

                var context = new GameSceneLaunchContext(elympicsClient, elympicsServer, gameConfig, elympicsBehavioursManager, snapshotRetriever, initializer =>
                {
                    _gameSceneInitializer = initializer;
                    initializer.LifecycleSink = sink;
                    session?.TrackCollectable(initializer);
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
