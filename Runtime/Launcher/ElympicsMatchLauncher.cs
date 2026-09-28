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

        internal static ElympicsMatchLauncher? Current { get; set; }

        // Keeps the static correct with domain reload disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;

        public MatchLauncherState State { get; private set; } = MatchLauncherState.Idle;
        public bool IsCurrentlyInMatch => State is MatchLauncherState.Loading or MatchLauncherState.Running or MatchLauncherState.Ended;

        public event Action<ElympicsMatchEndedInfo>? MatchEnded;
        public event Action<ElympicsDisconnectedInfo>? Disconnected;
        public event Action<MatchLauncherState>? StateChanged;

        /// <summary>Every event of the host protocol, as (type, requestId, payloadJson). Consumed by host bridges and editor tooling.</summary>
        internal event Action<string, string, string>? HostEventEmitted;

        /// <summary>The session a loading gameplay scene should pick up, or null if no launch is loading.</summary>
        internal ElympicsMatchSession? SessionAwaitingScene => State == MatchLauncherState.Loading ? _session : null;

        /// <summary>True while a launcher-driven match exists, i.e. the gameplay scene must not quit the application on its own.</summary>
        internal bool IsDrivingMatch => _session != null;

        internal string BootScene => _bootScene ??= ResolveBootScene();

        private readonly IElympicsSceneLoader _sceneLoader;
        private readonly Func<ElympicsConfig?> _loadConfig;
        private readonly bool _platformCanListen;
        private readonly Action<string> _exitApplication;
        private readonly LoggerConfig _logger = ElympicsLogger.WithClass(typeof(ElympicsMatchLauncher));

        private string? _bootScene;
        private ElympicsMatchSession? _session;
        private UniTaskCompletionSource<ElympicsLaunchResult>? _launchCompletion;
        private CancellationTokenSource? _timerCts;
        private UniTask _sceneLoad = UniTask.CompletedTask;
        private string _teardownCause = string.Empty;
        private readonly List<(string RequestId, UniTaskCompletionSource<ElympicsAbortResult> Completion)> _abortWaiters = new();
        private bool _quitting;

        internal ElympicsMatchLauncher(IElympicsSceneLoader sceneLoader, Func<ElympicsConfig?> loadConfig, bool platformCanListen, Action<string> exitApplication)
        {
            _sceneLoader = sceneLoader;
            _loadConfig = loadConfig;
            _platformCanListen = platformCanListen;
            _exitApplication = exitApplication;
        }

        #region Launch

        public UniTask<ElympicsLaunchResult> LaunchMatchAsync(ElympicsLaunchRequest request, CancellationToken ct = default) =>
            LaunchAsync(request, ElympicsLaunchSource.InProcess, ct);

        internal async UniTask<ElympicsLaunchResult> LaunchAsync(ElympicsLaunchRequest? request, ElympicsLaunchSource source, CancellationToken ct)
        {
            await UniTask.SwitchToMainThread();

            var requestId = request != null && !string.IsNullOrEmpty(request.requestId) ? request.requestId : Guid.NewGuid().ToString("N");
            if (request != null)
                request.requestId = requestId;

            if (_quitting)
                return RejectLaunch(requestId, ElympicsLaunchErrorCode.Aborted, "The application is quitting.");
            if (State != MatchLauncherState.Idle)
                return RejectLaunch(requestId, ElympicsLaunchErrorCode.Busy, $"A match is already {State}. Abort it and wait for idle before launching another one.");

            var games = (IReadOnlyList<ElympicsGameConfig?>?)_loadConfig()?.AvailableGames ?? Array.Empty<ElympicsGameConfig?>();
            if (!ElympicsLaunchRequestParser.TryParse(request, games, source, _platformCanListen, out var launch, out var failure))
                return RejectLaunch(requestId, failure!.Code, failure.message);

            var session = new ElympicsMatchSession(launch!, this);
            var completion = new UniTaskCompletionSource<ElympicsLaunchResult>();
            _session = session;
            _launchCompletion = completion;
            SetState(MatchLauncherState.Loading);
            _logger.LogInfo($"Launching {launch!.MatchMode} match of {launch.GameConfig.GameName} (request {requestId}).");
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
                FailLaunch(ElympicsLaunchErrorCode.Timeout, $"The match was not joined within {session.Launch.LaunchTimeout.TotalSeconds:0.#} s.");
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
            var matchId = (launch.MatchData?.MatchId ?? Guid.Empty).ToString();
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
                    var info = new ElympicsDisconnectedInfo { matchId = (_session?.Launch.MatchData?.MatchId ?? Guid.Empty).ToString(), byServer = byServer };
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
                new ElympicsMatchEndedInfo { matchId = (session.Launch.MatchData?.MatchId ?? Guid.Empty).ToString() });
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

            session?.Detach();
            _session = null;

            try
            {
                var bootScene = BootScene;
                if (_sceneLoader.ActiveScenePath == bootScene)
                    _logger.LogInfo("Still in the boot scene, not reloading it.");
                else if (!_quitting && !await _sceneLoader.LoadSingleAsync(bootScene))
                    _logger.LogError($"Could not load boot scene {bootScene}. Set {nameof(ElympicsConfig)}.bootScene and add it to Build Settings.");
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }

            try
            {
                await _sceneLoader.ReleaseMemoryAsync();
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

        internal void HandleHostLaunch(string requestJson)
        {
            ElympicsLaunchRequest? request = null;
            try
            {
                request = JsonUtility.FromJson<ElympicsLaunchRequest>(requestJson);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Malformed launch request JSON: {e.Message}");
            }
            if (request == null)
            {
                _ = RejectLaunch(string.Empty, ElympicsLaunchErrorCode.InvalidPayload, "The launch request is not valid JSON.");
                return;
            }
            LaunchAsync(request, ElympicsLaunchSource.Wire, CancellationToken.None).Forget();
        }

        internal void HandleHostAbort(string requestId) => AbortAsync(requestId, CancellationToken.None).Forget();

        internal void HandleHostPing(string requestId) => Emit(ElympicsHostProtocol.Events.Pong, requestId);

        internal void HandleHostQuit(string reason) => QuitApplication(reason);

        internal void EmitReady()
        {
            var games = _loadConfig()?.AvailableGames ?? Array.Empty<ElympicsGameConfig>();
            Emit(ElympicsHostProtocol.Events.Ready, string.Empty, new ElympicsReadyInfo
            {
                sdkVersion = ElympicsConfig.SdkVersion,
                protocolVersion = ElympicsHostProtocol.Version,
                gameIds = games.Where(game => game != null).Select(game => game.GameId).ToArray(),
            });
        }

        #endregion

        private string ResolveBootScene()
        {
            var configured = _loadConfig()?.BootScene ?? string.Empty;
            if (_sceneLoader.IsInBuild(configured))
                return configured;
            var fallback = _sceneLoader.ActiveScenePath;
            _logger.LogError(string.IsNullOrEmpty(configured)
                ? $"{nameof(ElympicsConfig)}.bootScene is not set. The match launcher will return to {fallback} after matches."
                : $"Boot scene {configured} is not in Build Settings. The match launcher will return to {fallback} after matches.");
            return fallback;
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
