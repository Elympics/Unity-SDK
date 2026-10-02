#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>Call from the main thread.</summary>
    [PublicAPI]
    public interface IElympicsMatchLauncher
    {
        MatchLauncherState State { get; }

        bool IsCurrentlyInMatch { get; }

        // Launch*Async completes when the match is joined or the launch fails. It is accepted only in MatchLauncherState.Idle,
        // otherwise it fails with ElympicsLaunchErrorCode.Busy. Its ct only stops waiting: use AbortMatchAsync to cancel the launch.

        UniTask<ElympicsLaunchResult> LaunchOnlineAsync(string gameId, OnlineLaunchData data, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        UniTask<ElympicsLaunchResult> LaunchSinglePlayerAsync(string gameId, SinglePlayerLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        /// <param name="data">Null means all values from <see cref="ElympicsGameConfig"/>.</param>
        UniTask<ElympicsLaunchResult> LaunchHalfRemoteClientAsync(string gameId, HalfRemoteClientLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        /// <summary>Fails with <see cref="ElympicsLaunchErrorCode.UnsupportedMode"/> on WebGL and iOS.</summary>
        /// <param name="data">Null means all values from <see cref="ElympicsGameConfig"/>.</param>
        UniTask<ElympicsLaunchResult> LaunchHalfRemoteServerAsync(string gameId, HalfRemoteServerLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        UniTask<ElympicsLaunchResult> LaunchSnapshotReplayAsync(string gameId, SnapshotReplayLaunchData data, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        /// <summary>Accepted in any state; completes in <see cref="MatchLauncherState.Idle"/>, immediately if already there.</summary>
        /// <param name="ct">Stops waiting for the result. Does not cancel the abort.</param>
        UniTask<ElympicsAbortResult> AbortMatchAsync(CancellationToken ct = default);

        /// <summary>
        /// Exits after at most <see cref="ElympicsLaunchDefaults.QuitGraceSeconds"/> of teardown. Prefer <see cref="AbortMatchAsync"/>.
        /// On WebGL the page survives: the host has to call <c>unityInstance.Quit()</c>.
        /// </summary>
        void QuitApplication(string reason = "");

        event Action<ElympicsMatchEndedInfo>? MatchEnded;

        event Action<ElympicsDisconnectedInfo>? Disconnected;

        event Action<MatchLauncherState>? StateChanged;
    }
}
