#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.SnapshotAnalysis.Retrievers;
using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>Call from the main thread.</summary>
    [PublicAPI]
    public abstract class ElympicsMatchLauncher
    {
        /// <summary>Available from the first scene's <c>Awake</c> on.</summary>
        public static ElympicsMatchLauncher Instance =>
            Current ?? throw new InvalidOperationException($"{nameof(ElympicsMatchLauncher)} is not created yet. It is created before the first scene loads in play mode.");

        internal static ElympicsMatchLauncher? Current { get; set; }

        // Only this SDK derives from this class.
        internal ElympicsMatchLauncher()
        { }

        public abstract MatchLauncherState State { get; }

        public bool IsCurrentlyInMatch => State is MatchLauncherState.Loading or MatchLauncherState.Running or MatchLauncherState.Ended;

        // Launch*Async completes when the match is joined or the launch fails. It is accepted only in MatchLauncherState.Idle,
        // otherwise it fails with ElympicsLaunchErrorCode.Busy. Its ct only stops waiting: use ReturnToBootAsync to cancel the launch.

        public UniTask<ElympicsLaunchResult> LaunchOnlineAsync(string gameId, OnlineLaunchData data, ElympicsLaunchOptions? options = null, CancellationToken ct = default) =>
            LaunchAsync(gameId, data, options, ct);

        public UniTask<ElympicsLaunchResult> LaunchSinglePlayerAsync(string gameId, SinglePlayerLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default) =>
            LaunchAsync(gameId, data ?? new SinglePlayerLaunchData(), options, ct);

        /// <param name="data">Null means all values from <see cref="ElympicsGameConfig"/>.</param>
        public UniTask<ElympicsLaunchResult> LaunchHalfRemoteClientAsync(string gameId, HalfRemoteClientLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default) =>
            LaunchAsync(gameId, data ?? new HalfRemoteClientLaunchData(), options, ct);

        /// <summary>Fails with <see cref="ElympicsLaunchErrorCode.UnsupportedMode"/> on WebGL and iOS.</summary>
        /// <param name="data">Null means all values from <see cref="ElympicsGameConfig"/>.</param>
        public UniTask<ElympicsLaunchResult> LaunchHalfRemoteServerAsync(string gameId, HalfRemoteServerLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default) =>
            LaunchAsync(gameId, data ?? new HalfRemoteServerLaunchData(), options, ct);

        public UniTask<ElympicsLaunchResult> LaunchSnapshotReplayAsync(string gameId, SnapshotReplayLaunchData data, ElympicsLaunchOptions? options = null, CancellationToken ct = default) =>
            LaunchAsync(gameId, data, options, ct);

        /// <summary>Launches a replay that is already in memory, e.g. one received from a web host.</summary>
        internal UniTask<ElympicsLaunchResult> LaunchSnapshotReplayAsync(string gameId, SnapshotAnalysisRetriever retriever, ElympicsLaunchOptions? options = null, CancellationToken ct = default) =>
            LaunchSnapshotReplayAsync(gameId, new SnapshotReplayLaunchData(retriever), options, ct);

        internal abstract UniTask<ElympicsLaunchResult> LaunchAsync(string? gameId, LaunchData? data, ElympicsLaunchOptions? options, CancellationToken ct);

        /// <summary>
        /// Ends the match, whether it has finished or is still loading or running, and loads the boot scene.
        /// Accepted in any state; completes in <see cref="MatchLauncherState.Idle"/>, immediately if already there.
        /// </summary>
        /// <param name="ct">Stops waiting for the result. Does not cancel the return to boot.</param>
        public UniTask<ElympicsReturnToBootResult> ReturnToBootAsync(CancellationToken ct = default) => ReturnToBootAsync(Guid.NewGuid().ToString("N"), ct);

        /// <param name="requestId">Empty means a generated one.</param>
        internal abstract UniTask<ElympicsReturnToBootResult> ReturnToBootAsync(string requestId, CancellationToken ct);

        /// <summary>
        /// Exits after at most <see cref="ElympicsLaunchDefaults.QuitGraceSeconds"/> of teardown. Prefer <see cref="ReturnToBootAsync(CancellationToken)"/>.
        /// On WebGL the page survives: the host has to call <c>unityInstance.Quit()</c>.
        /// </summary>
        public abstract void QuitApplication(string reason = "");

        public abstract event Action<ElympicsMatchEndedInfo>? MatchEnded;

        public abstract event Action<ElympicsDisconnectedInfo>? Disconnected;

        public abstract event Action<MatchLauncherState>? StateChanged;

        /// <summary>The data of the match being launched, run or torn down. Null when idle.</summary>
        internal abstract LaunchData? ActiveLaunchData { get; }

        /// <summary>Every host protocol event, as (type, requestId, payloadJson).</summary>
        internal abstract event Action<string, string, string>? HostEventEmitted;

        /// <summary>
        /// Launch commands take (game ID, data JSON, options JSON); the others take one string in <paramref name="first"/>.
        /// Unused strings are empty. An empty JSON string means defaults.
        /// </summary>
        internal abstract void HandleHostCommand(ElympicsHostCommandKind kind, string first, string second = "", string third = "");

        internal abstract void HandleHostLaunch(ElympicsHostLaunchKind kind, string gameId, string dataJson, string optionsJson);

        internal abstract void EmitReady();

        internal enum SceneAttachment
        {
            Attached,
            /// <summary>The scene was opened directly (e.g. Play pressed in the Editor).</summary>
            NotLauncherDriven,
            /// <summary>The match is being torn down or already has its scene. The scene must not initialize at all.</summary>
            Suppressed,
        }

        internal abstract SceneAttachment AttachGameplayScene(out ElympicsMatchSession? session);
    }
}
