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

        internal static DefaultElympicsMatchLauncher? Current { get; set; }

        // Only this SDK derives from this class.
        internal ElympicsMatchLauncher()
        { }

        public abstract MatchLauncherState State { get; }

        public abstract bool IsCurrentlyInMatch { get; }

        // Launch*Async completes when the match is joined or the launch fails. It is accepted only in MatchLauncherState.Idle,
        // otherwise it fails with ElympicsLaunchErrorCode.Busy. Its ct only stops waiting: use ReturnToBootAsync to cancel the launch.

        public abstract UniTask<ElympicsLaunchResult> LaunchOnlineAsync(string gameId, OnlineLaunchData data, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        public abstract UniTask<ElympicsLaunchResult> LaunchSinglePlayerAsync(string gameId, SinglePlayerLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        /// <param name="data">Null means all values from <see cref="ElympicsGameConfig"/>.</param>
        public abstract UniTask<ElympicsLaunchResult> LaunchHalfRemoteClientAsync(string gameId, HalfRemoteClientLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        /// <summary>Fails with <see cref="ElympicsLaunchErrorCode.UnsupportedMode"/> on WebGL and iOS.</summary>
        /// <param name="data">Null means all values from <see cref="ElympicsGameConfig"/>.</param>
        public abstract UniTask<ElympicsLaunchResult> LaunchHalfRemoteServerAsync(string gameId, HalfRemoteServerLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        public abstract UniTask<ElympicsLaunchResult> LaunchSnapshotReplayAsync(string gameId, SnapshotReplayLaunchData data, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        /// <summary>Launches a replay that is already in memory, e.g. one received from a web host.</summary>
        internal UniTask<ElympicsLaunchResult> LaunchSnapshotReplayAsync(string gameId, SnapshotAnalysisRetriever retriever, ElympicsLaunchOptions? options = null, CancellationToken ct = default) =>
            LaunchSnapshotReplayAsync(gameId, new SnapshotReplayLaunchData(retriever), options, ct);

        /// <summary>
        /// Ends the match, whether it has finished or is still loading or running, and loads the boot scene.
        /// Accepted in any state; completes in <see cref="MatchLauncherState.Idle"/>, immediately if already there.
        /// </summary>
        /// <param name="ct">Stops waiting for the result. Does not cancel the return to boot.</param>
        public abstract UniTask<ElympicsReturnToBootResult> ReturnToBootAsync(CancellationToken ct = default);

        /// <summary>
        /// Exits after at most <see cref="ElympicsLaunchDefaults.QuitGraceSeconds"/> of teardown. Prefer <see cref="ReturnToBootAsync"/>.
        /// On WebGL the page survives: the host has to call <c>unityInstance.Quit()</c>.
        /// </summary>
        public abstract void QuitApplication(string reason = "");

        public abstract event Action<ElympicsMatchEndedInfo>? MatchEnded;

        public abstract event Action<ElympicsDisconnectedInfo>? Disconnected;

        public abstract event Action<MatchLauncherState>? StateChanged;

        /// <summary>The data of the match being launched, run or torn down. Null when idle.</summary>
        internal abstract LaunchData? ActiveLaunchData { get; }
    }
}
