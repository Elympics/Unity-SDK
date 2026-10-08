#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Core.Logger;
using Elympics.SnapshotAnalysis.Retrievers;
using JetBrains.Annotations;
using UnityEngine;

namespace Elympics
{
    /// <summary>
    /// Created before the first scene loads and kept for the whole process. State transitions run synchronously
    /// on the main thread, so commands are serialized without a queue.
    /// </summary>
    [PublicAPI]
    public sealed class ElympicsMatchLauncher : IElympicsMatchLauncher, IElympicsMatchLifecycleSink
    {
        /// <summary>Available from the first scene's <c>Awake</c> on.</summary>
        public static IElympicsMatchLauncher Instance =>
            Current ?? throw new InvalidOperationException($"{nameof(ElympicsMatchLauncher)} is not created yet. It is created before the first scene loads in play mode.");

        internal static ElympicsMatchLauncher? Current { get; set; }

        public MatchLauncherState State { get; private set; } = MatchLauncherState.Idle;
        public bool IsCurrentlyInMatch => State is MatchLauncherState.Loading or MatchLauncherState.Running or MatchLauncherState.Ended;

        public event Action<ElympicsMatchEndedInfo>? MatchEnded;
        public event Action<ElympicsDisconnectedInfo>? Disconnected;
        public event Action<MatchLauncherState>? StateChanged;

        /// <summary>Every host protocol event, as (type, requestId, payloadJson).</summary>
        internal event Action<string, string, string>? HostEventEmitted;

        internal const string LimitedToParameterMatchMessage = "This process is limited to the match set by ELYMPICS_LAUNCH_MODE.";

        internal enum SceneAttachment
        {
            Attached,
            /// <summary>The scene was opened directly (e.g. Play pressed in the Editor).</summary>
            NotLauncherDriven,
            /// <summary>The match is being torn down or already has its scene. The scene must not initialize at all.</summary>
            Suppressed,
        }

        internal ElympicsMatchSession? SessionAwaitingScene => State == MatchLauncherState.Loading ? _session : null;

        internal SceneAttachment AttachGameplayScene(out ElympicsMatchSession? session)
        {
            session = SessionAwaitingScene;
            if (session != null && session.TryAttachScene())
                return SceneAttachment.Attached;
            session = null;
            // E.g. the launch was aborted or timed out while its scene was still loading: the scene activates during teardown.
            return IsDrivingMatch ? SceneAttachment.Suppressed : SceneAttachment.NotLauncherDriven;
        }

        /// <summary>While true, the gameplay scene must not quit the application on its own.</summary>
        internal bool IsDrivingMatch => _session != null;

        internal BootTarget Boot => _boot ??= ResolveBootTarget();

        internal sealed class BootTarget
        {
            public string? ScenePath { get; }
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
        private readonly bool _limitedToParameterMatch;
        private ElympicsProcessGlobals? _processGlobals;
        private int _cycle;
        private readonly LoggerConfig _logger = ElympicsLogger.WithClass(typeof(ElympicsMatchLauncher));

        private BootTarget? _boot;
        private ElympicsMatchSession? _session;
        private UniTaskCompletionSource<ElympicsLaunchResult>? _launchCompletion;
        private CancellationTokenSource? _timerCts;
        private UniTask _sceneLoad = UniTask.CompletedTask;
        private string _teardownCause = string.Empty;
        private readonly List<(string RequestId, UniTaskCompletionSource<ElympicsReturnToBootResult> Completion)> _returnToBootWaiters = new();
        private bool _quitting;

        internal ElympicsMatchLauncher(IElympicsSceneLoader sceneLoader, Func<ElympicsConfig?> loadConfig, ElympicsLaunchPlatform platform, Action<string> exitApplication,
            bool verifyTeardown = false, bool limitedToParameterMatch = false)
        {
            _limitedToParameterMatch = limitedToParameterMatch;
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

        /// <summary>Launches a replay that is already in memory, e.g. one received from a web host.</summary>
        internal UniTask<ElympicsLaunchResult> LaunchSnapshotReplayAsync(string gameId, SnapshotAnalysisRetriever retriever, ElympicsLaunchOptions? options, CancellationToken ct) =>
            LaunchAsync(gameId, new SnapshotReplayLaunchData(retriever), options, ct);

        internal async UniTask<ElympicsLaunchResult> LaunchAsync(string? gameId, LaunchData? data, ElympicsLaunchOptions? options, CancellationToken ct)
        {
            await UniTask.SwitchToMainThread();

            var requestId = string.IsNullOrEmpty(options?.RequestId) ? Guid.NewGuid().ToString("N") : options!.RequestId!;

            if (_quitting)
                return RejectLaunch(requestId, ElympicsLaunchErrorCode.Aborted, "The application is quitting.");
            if (_limitedToParameterMatch)
                return RejectLaunch(requestId, ElympicsLaunchErrorCode.Busy, LimitedToParameterMatchMessage);
            if (State != MatchLauncherState.Idle)
                return RejectLaunch(requestId, ElympicsLaunchErrorCode.Busy, $"A match is already {State}. Return to boot and wait for idle before launching another one.");

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
                    var result = replay.Preloaded != null
                        ? SnapshotReplayLoader.Check(replay.Preloaded, session.Launch.GameConfig)
                        : await SnapshotReplayLoader.LoadAsync(replay, session.Launch.GameConfig, session.Token);
                    if (session != _session || State != MatchLauncherState.Loading)
                        return;
                    if (result.Failure != null)
                    {
                        FailLaunch(result.Failure.Code, result.Failure.message);
                        return;
                    }
                    session.SnapshotRetriever = result.Retriever;
                }
                var loaded = await _sceneLoader.LoadSingleAsync(scenePath);
                if (session != _session || State != MatchLauncherState.Loading)
                    return;
                if (!loaded)
                    FailLaunch(ElympicsLaunchErrorCode.SceneLoadFailed, $"Could not load gameplay scene {scenePath}. Make sure it is in Build Settings.");
                else if (!session.SceneInitialized)
                    FailLaunch(ElympicsLaunchErrorCode.SceneLoadFailed, $"Gameplay scene {scenePath} has no {nameof(GameSceneManager)}.");
            }
            catch (OperationCanceledException) when (session.Token.IsCancellationRequested)
            {
                // Torn down while loading the replay: teardown reports the outcome.
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
            ReportLaunchFailed(requestId, code, message);
            BeginTeardown(requestId);
        }

        private void ReportLaunchFailed(string requestId, ElympicsLaunchErrorCode code, string message)
        {
            Emit(ElympicsHostProtocol.Events.LaunchFailed, requestId, new ElympicsLaunchFailure(code, message));
            CompleteLaunch(ElympicsLaunchResult.Failed(requestId, code, message));
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

        #region Return to boot and teardown

        public UniTask<ElympicsReturnToBootResult> ReturnToBootAsync(CancellationToken ct = default) => ReturnToBootAsync(Guid.NewGuid().ToString("N"), ct);

        internal async UniTask<ElympicsReturnToBootResult> ReturnToBootAsync(string requestId, CancellationToken ct)
        {
            await UniTask.SwitchToMainThread();
            if (string.IsNullOrEmpty(requestId))
                requestId = Guid.NewGuid().ToString("N");
            try
            {
                if (State == MatchLauncherState.Idle)
                {
                    Emit(ElympicsHostProtocol.Events.ReturnToBootAccepted, requestId);
                    Emit(ElympicsHostProtocol.Events.Idle, requestId);
                    return ElympicsReturnToBootResult.Succeeded(requestId);
                }

                var completion = new UniTaskCompletionSource<ElympicsReturnToBootResult>();
                _returnToBootWaiters.Add((requestId, completion));
                Emit(ElympicsHostProtocol.Events.ReturnToBootAccepted, requestId);

                if (State == MatchLauncherState.Loading && _session != null)
                    ReportLaunchFailed(_session.Launch.RequestId, ElympicsLaunchErrorCode.Aborted, "The launch was aborted.");

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
                Emit(ElympicsHostProtocol.Events.ReturnToBootFailed, requestId, new ElympicsLaunchFailure(ElympicsLaunchErrorCode.InternalError, message));
                return ElympicsReturnToBootResult.Failed(requestId, ElympicsLaunchErrorCode.InternalError, message);
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

            // Per-scene steps first, then process-wide state.
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
                    await LoadBootSceneAsync();
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }

            try
            {
                await _sceneLoader.ReleaseMemoryAsync(_verifyTeardown);
                if (_verifyTeardown)
                    ElympicsMatchTeardown.AssertReleased(session?.Collectable ?? Array.Empty<WeakReference>(), ++_cycle);
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }

            SetState(MatchLauncherState.Idle);
            var idleRequestIds = new List<string> { _teardownCause };
            idleRequestIds.AddRange(_returnToBootWaiters.Select(waiter => waiter.RequestId));
            foreach (var requestId in idleRequestIds.Distinct())
                Emit(ElympicsHostProtocol.Events.Idle, requestId);
            CompleteReturnToBootWaiters();
        }

        private void CompleteReturnToBootWaiters()
        {
            var waiters = _returnToBootWaiters.ToArray();
            _returnToBootWaiters.Clear();
            foreach (var (requestId, completion) in waiters)
                _ = completion.TrySetResult(ElympicsReturnToBootResult.Succeeded(requestId));
        }

        private async UniTask LoadBootSceneAsync()
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
                ReportLaunchFailed(session.Launch.RequestId, ElympicsLaunchErrorCode.Aborted, "The application is quitting.");
            CompleteReturnToBootWaiters();

            Emit(ElympicsHostProtocol.Events.Quitting, string.Empty, new ElympicsQuitInfo { reason = reason ?? string.Empty });
            CancelTimer();
            QuitAsync(session, reason ?? string.Empty).Forget();
        }

        private async UniTaskVoid QuitAsync(ElympicsMatchSession? session, string reason)
        {
            try
            {
                // Best effort, so the game server sees a clean disconnect.
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

        /// <summary>An empty JSON string means defaults.</summary>
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

        /// <summary>
        /// Launch commands take (game ID, data JSON, options JSON); the others take one string in <paramref name="first"/>.
        /// Unused strings are empty.
        /// </summary>
        internal void HandleHostCommand(ElympicsHostCommandKind kind, string first, string second = "", string third = "")
        {
            switch (kind)
            {
                case ElympicsHostCommandKind.LaunchOnline:
                    HandleHostLaunch(ElympicsHostLaunchKind.Online, first, second, third);
                    break;
                case ElympicsHostCommandKind.LaunchSinglePlayer:
                    HandleHostLaunch(ElympicsHostLaunchKind.SinglePlayer, first, second, third);
                    break;
                case ElympicsHostCommandKind.LaunchHalfRemoteClient:
                    HandleHostLaunch(ElympicsHostLaunchKind.HalfRemoteClient, first, second, third);
                    break;
                case ElympicsHostCommandKind.LaunchHalfRemoteServer:
                    HandleHostLaunch(ElympicsHostLaunchKind.HalfRemoteServer, first, second, third);
                    break;
                case ElympicsHostCommandKind.LaunchSnapshotReplay:
                    HandleHostLaunch(ElympicsHostLaunchKind.SnapshotReplay, first, second, third);
                    break;
                case ElympicsHostCommandKind.ReturnToBoot:
                    HandleHostReturnToBoot(first);
                    break;
                case ElympicsHostCommandKind.QuitApplication:
                    HandleHostQuit(first);
                    break;
                case ElympicsHostCommandKind.Ping:
                    HandleHostPing(first);
                    break;
                case ElympicsHostCommandKind.None:
                default:
                    _logger.LogWarning($"Unknown host command {(int)kind} ignored.");
                    break;
            }
        }

        internal void HandleHostReturnToBoot(string requestId) => ReturnToBootAsync(requestId, CancellationToken.None).Forget();

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
            var warning = ElympicsBootScene.FindProblem(config, _sceneLoader.IsInBuild);
            if (warning == null)
                return BootTarget.Scene(config!.BootScene);
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
            _logger.LogDebug($"Host event {type}" + (requestId.Length > 0 ? $" (request {requestId})" : "") + (payloadJson.Length > 0 ? $": {payloadJson}" : ""));
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
