#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Core.Logger;
using JetBrains.Annotations;
using UnityEngine;

namespace Elympics
{
    /// <summary>
    /// Host-driven match launcher. Created automatically before the first scene loads and kept alive for the whole process.
    /// </summary>
    /// <remarks>
    /// All state transitions happen synchronously on the main thread, so commands are serialized without a queue:
    /// a launch is accepted only in <see cref="MatchLauncherState.Idle"/>, and an abort is accepted in every state.
    /// The launcher owns nothing per match except the <see cref="ElympicsMatchSession"/>, which is detached in teardown.
    /// </remarks>
    [PublicAPI]
    public sealed class ElympicsMatchLauncher : IElympicsMatchLauncher, IElympicsMatchLifecycleSink
    {
        /// <summary>The launcher of this process. Available from the first scene's <c>Awake</c> on.</summary>
        public static IElympicsMatchLauncher Instance =>
            Current ?? throw new InvalidOperationException($"{nameof(ElympicsMatchLauncher)} is not created yet. It is created before the first scene loads in play mode.");

        /// <summary>Set and cleared by <see cref="ElympicsMatchLauncherBootstrap"/>.</summary>
        internal static ElympicsMatchLauncher? Current { get; set; }

        public MatchLauncherState State { get; private set; } = MatchLauncherState.Idle;
        public bool IsCurrentlyInMatch => State is MatchLauncherState.Loading or MatchLauncherState.Running or MatchLauncherState.Ended;

        public event Action<ElympicsMatchEndedInfo>? MatchEnded;
        public event Action<ElympicsDisconnectedInfo>? Disconnected;
        public event Action<MatchLauncherState>? StateChanged;

        /// <summary>Every event of the host protocol, as (type, requestId, payloadJson). Consumed by host bridges and editor tooling.</summary>
        internal event Action<string, string, string>? HostEventEmitted;

        internal enum SceneAttachment
        {
            /// <summary>The scene was loaded by the launcher and must initialize for the returned session.</summary>
            Attached,
            /// <summary>No launcher-driven match: the scene was opened directly (e.g. Play pressed in the Editor) and uses its debug mode.</summary>
            NotLauncherDriven,
            /// <summary>A launcher-driven match is being torn down or already has its scene. The scene must not initialize at all.</summary>
            Suppressed,
        }

        /// <summary>The session a loading gameplay scene should pick up, or null if no launch is loading.</summary>
        internal ElympicsMatchSession? SessionAwaitingScene => State == MatchLauncherState.Loading ? _session : null;

        /// <summary>Called by a gameplay scene on load to decide how it initializes.</summary>
        internal SceneAttachment AttachGameplayScene(out ElympicsMatchSession? session)
        {
            session = SessionAwaitingScene;
            if (session != null && session.TryAttachScene())
                return SceneAttachment.Attached;
            session = null;
            // E.g. the launch was aborted or timed out while its scene was still loading: the scene activates during teardown.
            return IsDrivingMatch ? SceneAttachment.Suppressed : SceneAttachment.NotLauncherDriven;
        }

        /// <summary>True while a launcher-driven match exists, i.e. the gameplay scene must not quit the application on its own.</summary>
        internal bool IsDrivingMatch => _session != null;

        /// <summary>Where teardown returns to. Resolved once, on first use.</summary>
        internal BootTarget Boot => _boot ??= ResolveBootTarget();

        /// <summary>The configured boot scene, or a generated empty scene if the configured one is unusable.</summary>
        internal sealed class BootTarget
        {
            /// <summary>The boot scene path, or null to return to a generated empty scene.</summary>
            public string? ScenePath { get; }
            /// <summary>Why the configured boot scene is not used, or null if it is.</summary>
            public string? Warning { get; }

            private BootTarget(string? scenePath, string? warning)
            {
                ScenePath = scenePath;
                Warning = warning;
            }

            public static BootTarget Scene(string scenePath) => new(scenePath, null);
            public static BootTarget Empty(string warning) => new(null, warning);
        }

        private readonly IElympicsSceneLoader _sceneLoader;
        private readonly Func<ElympicsConfig?> _loadConfig;
        private readonly ElympicsLaunchPlatform _platform;
        private readonly Action<string> _exitApplication;
        private readonly bool _verifyTeardown;
        private ElympicsProcessGlobals? _processGlobals;
        private int _cycle;
        private readonly LoggerConfig _logger = ElympicsLogger.WithClass(typeof(ElympicsMatchLauncher));

        private BootTarget? _boot;
        private ElympicsMatchSession? _session;
        private UniTaskCompletionSource<ElympicsLaunchResult>? _launchCompletion;
        private CancellationTokenSource? _timerCts;
        private UniTask _sceneLoad = UniTask.CompletedTask;
        private string _teardownCause = string.Empty;
        private readonly List<(string RequestId, UniTaskCompletionSource<ElympicsAbortResult> Completion)> _abortWaiters = new();
        private bool _quitting;

        /// <param name="verifyTeardown">Run the post-teardown assertions and memory sample (development builds only).</param>
        internal ElympicsMatchLauncher(IElympicsSceneLoader sceneLoader, Func<ElympicsConfig?> loadConfig, ElympicsLaunchPlatform platform, Action<string> exitApplication, bool verifyTeardown = false)
        {
            _sceneLoader = sceneLoader;
            _loadConfig = loadConfig;
            _platform = platform;
            _exitApplication = exitApplication;
            _verifyTeardown = verifyTeardown;
        }

        #region Launch

        public UniTask<ElympicsLaunchResult> LaunchOnlineAsync(string gameId, OnlineLaunchData data, ElympicsLaunchOptions? options = null, CancellationToken ct = default) =>
            LaunchAsync(gameId, data, options, ct);

        public UniTask<ElympicsLaunchResult> LaunchSinglePlayerAsync(string gameId, SinglePlayerLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default) =>
            LaunchAsync(gameId, data ?? new SinglePlayerLaunchData(), options, ct);

        public UniTask<ElympicsLaunchResult> LaunchHalfRemoteClientAsync(string gameId, HalfRemoteClientLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default) =>
            LaunchAsync(gameId, data ?? new HalfRemoteClientLaunchData(), options, ct);

        public UniTask<ElympicsLaunchResult> LaunchHalfRemoteServerAsync(string gameId, HalfRemoteServerLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default) =>
            LaunchAsync(gameId, data ?? new HalfRemoteServerLaunchData(), options, ct);

        public UniTask<ElympicsLaunchResult> LaunchSnapshotReplayAsync(string gameId, SnapshotReplayLaunchData data, ElympicsLaunchOptions? options = null, CancellationToken ct = default) =>
            LaunchAsync(gameId, data, options, ct);

        internal async UniTask<ElympicsLaunchResult> LaunchAsync(string? gameId, LaunchData? data, ElympicsLaunchOptions? options, CancellationToken ct)
        {
            await UniTask.SwitchToMainThread();

            var requestId = string.IsNullOrEmpty(options?.RequestId) ? Guid.NewGuid().ToString("N") : options!.RequestId!;

            if (_quitting)
                return RejectLaunch(requestId, ElympicsLaunchErrorCode.Aborted, "The application is quitting.");
            if (State != MatchLauncherState.Idle)
                return RejectLaunch(requestId, ElympicsLaunchErrorCode.Busy, $"A match is already {State}. Abort it and wait for idle before launching another one.");

            var games = (IReadOnlyList<ElympicsGameConfig?>?)_loadConfig()?.AvailableGames ?? Array.Empty<ElympicsGameConfig?>();
            if (!ElympicsLaunchValidator.TryValidate(requestId, gameId, data, options, games, _platform, out var launch, out var failure))
                return RejectLaunch(requestId, failure!.Code, failure.message);

            var session = new ElympicsMatchSession(launch!, this);
            var completion = new UniTaskCompletionSource<ElympicsLaunchResult>();
            _session = session;
            _launchCompletion = completion;
            _processGlobals = ElympicsProcessGlobals.Capture();
            ElympicsConfig.UpdateLoggerState(launch!.GameConfig);
            if (launch.MatchId != Guid.Empty)
                ElympicsLogger.State.SetMatchId(launch.MatchId.ToString());
            SetState(MatchLauncherState.Loading);
            _logger.LogInfo($"Launching {launch.Data.ModeName} match of {launch.GameConfig.GameName} (request {requestId}).");
            Emit(ElympicsHostProtocol.Events.LaunchAccepted, requestId);
            ArmTimer(launch.LaunchTimeout, () => OnLaunchTimeout(session));
            _sceneLoad = LoadGameplaySceneAsync(session).Preserve();

            return await completion.Task.AttachExternalCancellation(ct);
        }

        private ElympicsLaunchResult RejectLaunch(string requestId, ElympicsLaunchErrorCode code, string message)
        {
            _logger.LogWarning($"Launch {requestId} rejected: {code}: {message}");
            Emit(ElympicsHostProtocol.Events.LaunchFailed, requestId, new ElympicsLaunchFailure(code, message));
            return ElympicsLaunchResult.Failed(requestId, code, message);
        }

        private async UniTask LoadGameplaySceneAsync(ElympicsMatchSession session)
        {
            var scenePath = session.Launch.GameConfig.GameplayScene;
            try
            {
                if (session.Launch.Data is SnapshotReplayLaunchData replay)
                {
                    if (!SnapshotReplayLoader.TryLoad(replay, session.Launch.GameConfig, out var retriever, out var failure))
                    {
                        FailLaunch(failure!.Code, failure.message);
                        return;
                    }
                    session.SnapshotRetriever = retriever;
                }
                var loaded = await _sceneLoader.LoadSingleAsync(scenePath);
                if (session != _session || State != MatchLauncherState.Loading)
                    return;
                if (!loaded)
                    FailLaunch(ElympicsLaunchErrorCode.SceneLoadFailed, $"Could not load gameplay scene {scenePath}. Make sure it is in Build Settings.");
                else if (!session.SceneInitialized)
                    FailLaunch(ElympicsLaunchErrorCode.SceneLoadFailed, $"Gameplay scene {scenePath} has no {nameof(GameSceneManager)}.");
            }
            catch (Exception e)
            {
                _logger.LogException(e);
                if (session == _session)
                    FailLaunch(ElympicsLaunchErrorCode.InternalError, $"{e.GetType().Name}: {e.Message}");
            }
        }

        private void FailLaunch(ElympicsLaunchErrorCode code, string message)
        {
            if (State != MatchLauncherState.Loading || _session == null)
                return;
            var requestId = _session.Launch.RequestId;
            _logger.LogWarning($"Launch {requestId} failed: {code}: {message}");
            Emit(ElympicsHostProtocol.Events.LaunchFailed, requestId, new ElympicsLaunchFailure(code, message));
            CompleteLaunch(ElympicsLaunchResult.Failed(requestId, code, message));
            BeginTeardown(requestId);
        }

        private void CompleteLaunch(ElympicsLaunchResult result)
        {
            var completion = _launchCompletion;
            _launchCompletion = null;
            _ = completion?.TrySetResult(result);
        }

        private void OnLaunchTimeout(ElympicsMatchSession session)
        {
            if (session == _session)
                FailLaunch(ElympicsLaunchErrorCode.Timeout, $"The match was not joined within {session.Launch.LaunchTimeout?.TotalSeconds:0.#} s.");
        }

        #endregion

        #region Reports from the gameplay scene

        void IElympicsMatchLifecycleSink.SceneInitializationFailed(ElympicsLaunchErrorCode code, string message) => FailLaunch(code, message);

        void IElympicsMatchLifecycleSink.MatchJoined()
        {
            if (State != MatchLauncherState.Loading || _session == null)
                return;
            CancelTimer();
            SetState(MatchLauncherState.Running);
            var launch = _session.Launch;
            var matchId = launch.MatchId.ToString();
            _logger.LogInfo($"Launch {launch.RequestId} succeeded.");
            Emit(ElympicsHostProtocol.Events.LaunchSucceeded, launch.RequestId, new ElympicsLaunchSucceededInfo { gameId = launch.GameConfig.GameId, matchId = matchId });
            CompleteLaunch(ElympicsLaunchResult.Succeeded(launch.RequestId, launch.GameConfig.GameId, matchId));
        }

        void IElympicsMatchLifecycleSink.JoinFailed(string message)
        {
            if (State == MatchLauncherState.Loading)
                FailLaunch(ElympicsLaunchErrorCode.ConnectionFailed, message);
        }

        void IElympicsMatchLifecycleSink.MatchEnded(Guid matchId)
        {
            switch (State)
            {
                case MatchLauncherState.Loading:
                    FailLaunch(ElympicsLaunchErrorCode.ConnectionFailed, "The match ended before it was joined.");
                    return;
                case MatchLauncherState.Running:
                    var info = new ElympicsMatchEndedInfo { matchId = matchId.ToString() };
                    EnterEnded();
                    Emit(ElympicsHostProtocol.Events.MatchEnded, string.Empty, info);
                    Raise(MatchEnded, info);
                    return;
                case MatchLauncherState.Idle:
                case MatchLauncherState.Ended:
                case MatchLauncherState.Unloading:
                default:
                    return;
            }
        }

        void IElympicsMatchLifecycleSink.Disconnected(bool byServer)
        {
            switch (State)
            {
                case MatchLauncherState.Loading:
                    FailLaunch(ElympicsLaunchErrorCode.ConnectionFailed, $"Disconnected by {(byServer ? "server" : "client")} before the match was joined.");
                    return;
                case MatchLauncherState.Running:
                    var info = new ElympicsDisconnectedInfo { matchId = (_session?.Launch.MatchId ?? Guid.Empty).ToString(), byServer = byServer };
                    EnterEnded();
                    Emit(ElympicsHostProtocol.Events.Disconnected, string.Empty, info);
                    Raise(Disconnected, info);
                    return;
                case MatchLauncherState.Idle:
                case MatchLauncherState.Ended:
                case MatchLauncherState.Unloading:
                default:
                    return;
            }
        }

        private void EnterEnded()
        {
            var session = _session!;
            SetState(MatchLauncherState.Ended);
            ArmTimer(session.Launch.PostMatchTimeout, () => OnPostMatchTimeout(session));
        }

        private void OnPostMatchTimeout(ElympicsMatchSession session)
        {
            if (session != _session || State != MatchLauncherState.Ended)
                return;
            _logger.LogWarning($"The host did not return to boot within {session.Launch.PostMatchTimeout?.TotalSeconds:0.#} s after the match ended. Returning on its own.");
            Emit(ElympicsHostProtocol.Events.MatchEndTimedOut, string.Empty,
                new ElympicsMatchEndedInfo { matchId = session.Launch.MatchId.ToString() });
            BeginTeardown(string.Empty);
        }

        #endregion

        #region Abort and teardown

        public UniTask<ElympicsAbortResult> AbortMatchAsync(CancellationToken ct = default) => AbortAsync(Guid.NewGuid().ToString("N"), ct);

        internal async UniTask<ElympicsAbortResult> AbortAsync(string requestId, CancellationToken ct)
        {
            await UniTask.SwitchToMainThread();
            if (string.IsNullOrEmpty(requestId))
                requestId = Guid.NewGuid().ToString("N");
            try
            {
                if (State == MatchLauncherState.Idle)
                {
                    Emit(ElympicsHostProtocol.Events.AbortAccepted, requestId);
                    Emit(ElympicsHostProtocol.Events.Idle, requestId);
                    return ElympicsAbortResult.Succeeded(requestId);
                }

                var completion = new UniTaskCompletionSource<ElympicsAbortResult>();
                _abortWaiters.Add((requestId, completion));
                Emit(ElympicsHostProtocol.Events.AbortAccepted, requestId);

                if (State == MatchLauncherState.Loading && _session != null)
                {
                    var launchId = _session.Launch.RequestId;
                    const string message = "The launch was aborted.";
                    Emit(ElympicsHostProtocol.Events.LaunchFailed, launchId, new ElympicsLaunchFailure(ElympicsLaunchErrorCode.Aborted, message));
                    CompleteLaunch(ElympicsLaunchResult.Failed(launchId, ElympicsLaunchErrorCode.Aborted, message));
                }

                BeginTeardown(requestId);
                return await completion.Task.AttachExternalCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogException(e);
                var message = $"{e.GetType().Name}: {e.Message}";
                Emit(ElympicsHostProtocol.Events.AbortFailed, requestId, new ElympicsLaunchFailure(ElympicsLaunchErrorCode.InternalError, message));
                return ElympicsAbortResult.Failed(requestId, ElympicsLaunchErrorCode.InternalError, message);
            }
        }

        private void BeginTeardown(string causeRequestId)
        {
            if (State is MatchLauncherState.Unloading or MatchLauncherState.Idle)
                return;
            _teardownCause = causeRequestId;
            CancelTimer();
            SetState(MatchLauncherState.Unloading);
            TeardownAsync().Forget();
        }

        /// <summary>The only way back to <see cref="MatchLauncherState.Idle"/>. Every step is guarded, so it never throws out.</summary>
        private async UniTaskVoid TeardownAsync()
        {
            var session = _session;
            try
            {
                // A scene load cannot be cancelled; let it finish so the boot scene is loaded after it, not before.
                await _sceneLoad;
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }

            // Per-scene steps first (stop the simulation, dispose transport and initializer), then process-wide state.
            session?.Detach();
            _session = null;
            ElympicsMatchTeardown.ReleaseStatics();
            try
            {
                _processGlobals?.Restore();
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }
            _processGlobals = null;

            try
            {
                if (!_quitting)
                    await ReturnToBootAsync();
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }

            try
            {
                await _sceneLoader.ReleaseMemoryAsync();
                if (_verifyTeardown)
                    ElympicsMatchTeardown.AssertReleased(session?.Collectable ?? Array.Empty<WeakReference>(), ++_cycle);
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }

            SetState(MatchLauncherState.Idle);
            var idleRequestIds = new List<string> { _teardownCause };
            idleRequestIds.AddRange(_abortWaiters.Select(waiter => waiter.RequestId));
            foreach (var requestId in idleRequestIds.Distinct())
                Emit(ElympicsHostProtocol.Events.Idle, requestId);
            var waiters = _abortWaiters.ToArray();
            _abortWaiters.Clear();
            foreach (var (requestId, completion) in waiters)
                _ = completion.TrySetResult(ElympicsAbortResult.Succeeded(requestId));
        }

        private async UniTask ReturnToBootAsync()
        {
            var bootScene = Boot.ScenePath;
            if (bootScene == null)
                await _sceneLoader.ReturnToEmptySceneAsync();
            else if (_sceneLoader.ActiveScenePath == bootScene)
                _logger.LogInfo("Still in the boot scene, not reloading it.");
            else if (!await _sceneLoader.LoadSingleAsync(bootScene))
                _logger.LogError($"Could not load boot scene {bootScene}. Set {nameof(ElympicsConfig)}.bootScene and add it to Build Settings.");
        }

        #endregion

        #region Quit

        public void QuitApplication(string reason = "")
        {
            if (_quitting)
                return;
            _quitting = true;
            _logger.LogWarning($"Quitting the application on request of the host. Reason: {(string.IsNullOrEmpty(reason) ? "none given" : reason)}");

            var session = _session;
            if (State == MatchLauncherState.Loading && session != null)
            {
                const string message = "The application is quitting.";
                Emit(ElympicsHostProtocol.Events.LaunchFailed, session.Launch.RequestId, new ElympicsLaunchFailure(ElympicsLaunchErrorCode.Aborted, message));
                CompleteLaunch(ElympicsLaunchResult.Failed(session.Launch.RequestId, ElympicsLaunchErrorCode.Aborted, message));
            }
            var waiters = _abortWaiters.ToArray();
            _abortWaiters.Clear();
            foreach (var (requestId, completion) in waiters)
                _ = completion.TrySetResult(ElympicsAbortResult.Succeeded(requestId));

            Emit(ElympicsHostProtocol.Events.Quitting, string.Empty, new ElympicsQuitInfo { reason = reason ?? string.Empty });
            CancelTimer();
            QuitAsync(session, reason ?? string.Empty).Forget();
        }

        private async UniTaskVoid QuitAsync(ElympicsMatchSession? session, string reason)
        {
            try
            {
                // Best effort, so the game server sees a clean disconnect. Never let it block the exit.
                _ = await UniTask.WhenAny(
                    QuitTeardownAsync(session),
                    UniTask.Delay(TimeSpan.FromSeconds(ElympicsLaunchDefaults.QuitGraceSeconds), DelayType.Realtime));
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }
            _exitApplication(reason);
        }

        private async UniTask QuitTeardownAsync(ElympicsMatchSession? session)
        {
            await UniTask.Yield();
            session?.Detach();
        }

        #endregion

        #region Host commands

        /// <summary>Runs a launch command of a host bridge: the JSON of the mode's launch data and of the launch options, each empty for defaults.</summary>
        internal void HandleHostLaunch(ElympicsHostLaunchKind kind, string gameId, string dataJson, string optionsJson)
        {
            if (!ElympicsLaunchValidator.TryParseOptions(optionsJson, out var options, out var failure)
                || !ElympicsLaunchValidator.TryParseData(kind, dataJson, out var data, out failure))
            {
                _ = RejectLaunch(options?.RequestId ?? string.Empty, failure!.Code, failure.message);
                return;
            }
            LaunchAsync(gameId, data, options, CancellationToken.None).Forget();
        }

        /// <summary>Runs a command received by a host bridge. <paramref name="argument"/> is the command's only string argument.</summary>
        internal void HandleHostCommand(ElympicsHostCommandKind kind, string argument)
        {
            switch (kind)
            {
                case ElympicsHostCommandKind.LaunchMatch:
                    _ = RejectLaunch(string.Empty, ElympicsLaunchErrorCode.UnsupportedMode,
                        "The generic launchMatch command was removed. Per-mode launch commands (launchOnline, launchSinglePlayer, ...) come with the next host bridge version.");
                    break;
                case ElympicsHostCommandKind.AbortMatch:
                case ElympicsHostCommandKind.ReturnToBoot:
                    HandleHostAbort(argument);
                    break;
                case ElympicsHostCommandKind.QuitApplication:
                    HandleHostQuit(argument);
                    break;
                case ElympicsHostCommandKind.Ping:
                    HandleHostPing(argument);
                    break;
                case ElympicsHostCommandKind.None:
                default:
                    _logger.LogWarning($"Unknown host command {(int)kind} ignored.");
                    break;
            }
        }

        internal void HandleHostAbort(string requestId) => AbortAsync(requestId, CancellationToken.None).Forget();

        internal void HandleHostPing(string requestId) => Emit(ElympicsHostProtocol.Events.Pong, requestId);

        internal void HandleHostQuit(string reason) => QuitApplication(reason);

        internal void EmitReady()
        {
            var games = _loadConfig()?.AvailableGames ?? Array.Empty<ElympicsGameConfig>();
            var bootWarning = Boot.Warning;
            Emit(ElympicsHostProtocol.Events.Ready, string.Empty, new ElympicsReadyInfo
            {
                sdkVersion = ElympicsConfig.SdkVersion,
                protocolVersion = ElympicsHostProtocol.Version,
                gameIds = games.Where(game => game != null).Select(game => game.GameId).ToArray(),
                warnings = bootWarning != null ? new[] { bootWarning } : Array.Empty<string>(),
            });
        }

        #endregion

        private BootTarget ResolveBootTarget()
        {
            var config = _loadConfig();
            var configured = config?.BootScene ?? string.Empty;
            string? warning = null;
            if (string.IsNullOrEmpty(configured))
                warning = $"{nameof(ElympicsConfig)} has no boot scene, so matches return to an empty scene. "
                    + "Set ElympicsConfig > Boot scene to return to your own scene.";
            else if (!_sceneLoader.IsInBuild(configured))
                warning = $"Boot scene {configured} is not in Build Settings, so matches return to an empty scene. "
                    + "Set ElympicsConfig > Boot scene to a separate scene in Build Settings, or clear it.";
            else if (config!.AvailableGames.Any(game => game != null && game.GameplayScene == configured))
                warning = $"Boot scene {configured} is also a gameplay scene, so matches return to an empty scene. "
                    + "Set ElympicsConfig > Boot scene to a separate scene in Build Settings, or clear it.";

            if (warning == null)
                return BootTarget.Scene(configured);
            _logger.LogWarning(warning);
            return BootTarget.Empty(warning);
        }

        private void SetState(MatchLauncherState state)
        {
            State = state;
            Raise(StateChanged, state);
        }

        private void ArmTimer(TimeSpan? timeout, Action onExpired)
        {
            CancelTimer();
            if (timeout == null)
                return;
            _timerCts = new CancellationTokenSource();
            RunTimer(timeout.Value, onExpired, _timerCts.Token).Forget();
        }

        private void CancelTimer()
        {
            _timerCts?.Cancel();
            _timerCts?.Dispose();
            _timerCts = null;
        }

        private static async UniTaskVoid RunTimer(TimeSpan timeout, Action onExpired, CancellationToken ct)
        {
            if (await UniTask.Delay(timeout, DelayType.Realtime, cancellationToken: ct).SuppressCancellationThrow())
                return;
            onExpired();
        }

        private void Emit(string type, string requestId, object? payload = null)
        {
            var payloadJson = payload == null ? string.Empty : JsonUtility.ToJson(payload);
            try
            {
                HostEventEmitted?.Invoke(type, requestId, payloadJson);
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }
        }

        private void Raise<T>(Action<T>? handler, T arg)
        {
            try
            {
                handler?.Invoke(arg);
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }
        }
    }
}
