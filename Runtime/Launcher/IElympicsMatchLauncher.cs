#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>
    /// Starts matches on request of a host (PlayPad, a game's own menu, or an out-of-process host through a bridge)
    /// and returns to the boot scene after them. Available as <see cref="ElympicsMatchLauncher.Instance"/>.
    /// </summary>
    /// <remarks>Call from the main thread.</remarks>
    [PublicAPI]
    public interface IElympicsMatchLauncher
    {
        MatchLauncherState State { get; }

        /// <summary>True while the gameplay scene is loaded: <see cref="MatchLauncherState.Loading"/>, <see cref="MatchLauncherState.Running"/> or <see cref="MatchLauncherState.Ended"/>.</summary>
        bool IsCurrentlyInMatch { get; }

        // Every Launch*Async method loads the gameplay scene of the game with ID gameId and starts the match. It completes when the match
        // is joined or the launch fails, and is accepted only in MatchLauncherState.Idle (otherwise it fails with ElympicsLaunchErrorCode.Busy).

        /// <summary>Joins a match on an Elympics game server.</summary>
        /// <param name="ct">Stops waiting for the result. Does not cancel the launch — use <see cref="AbortMatchAsync"/> for that.</param>
        UniTask<ElympicsLaunchResult> LaunchOnlineAsync(string gameId, OnlineLaunchData data, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        /// <summary>Plays a match with the server simulated in this process.</summary>
        /// <param name="data">Null means all defaults.</param>
        /// <param name="ct">Stops waiting for the result. Does not cancel the launch — use <see cref="AbortMatchAsync"/> for that.</param>
        UniTask<ElympicsLaunchResult> LaunchSinglePlayerAsync(string gameId, SinglePlayerLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        /// <summary>Joins a half-remote server.</summary>
        /// <param name="data">Null means all values from <see cref="ElympicsGameConfig"/>.</param>
        /// <param name="ct">Stops waiting for the result. Does not cancel the launch — use <see cref="AbortMatchAsync"/> for that.</param>
        UniTask<ElympicsLaunchResult> LaunchHalfRemoteClientAsync(string gameId, HalfRemoteClientLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        /// <summary>Runs a half-remote server in this process. Fails with <see cref="ElympicsLaunchErrorCode.UnsupportedMode"/> on WebGL and iOS.</summary>
        /// <param name="data">Null means all values from <see cref="ElympicsGameConfig"/>.</param>
        /// <param name="ct">Stops waiting for the result. Does not cancel the launch — use <see cref="AbortMatchAsync"/> for that.</param>
        UniTask<ElympicsLaunchResult> LaunchHalfRemoteServerAsync(string gameId, HalfRemoteServerLaunchData? data = null, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        /// <summary>Replays a recorded match.</summary>
        /// <param name="ct">Stops waiting for the result. Does not cancel the launch — use <see cref="AbortMatchAsync"/> for that.</param>
        UniTask<ElympicsLaunchResult> LaunchSnapshotReplayAsync(string gameId, SnapshotReplayLaunchData data, ElympicsLaunchOptions? options = null, CancellationToken ct = default);

        /// <summary>
        /// Leaves the current match, whatever its state, and returns to the boot scene. Completes in <see cref="MatchLauncherState.Idle"/>.
        /// Idempotent: from <see cref="MatchLauncherState.Idle"/> it succeeds immediately.
        /// </summary>
        /// <param name="ct">Stops waiting for the result. Does not cancel the abort.</param>
        UniTask<ElympicsAbortResult> AbortMatchAsync(CancellationToken ct = default);

        /// <summary>
        /// Backup escape hatch: exits the application from any state, after at most <see cref="ElympicsLaunchDefaults.QuitGraceSeconds"/> of teardown.
        /// Prefer <see cref="AbortMatchAsync"/>. On WebGL the page survives — the host has to call <c>unityInstance.Quit()</c>.
        /// </summary>
        void QuitApplication(string reason = "");

        /// <summary>Raised when the server ends the match. The state becomes <see cref="MatchLauncherState.Ended"/>.</summary>
        event Action<ElympicsMatchEndedInfo>? MatchEnded;

        /// <summary>Raised when the connection is lost mid-match. The state becomes <see cref="MatchLauncherState.Ended"/>.</summary>
        event Action<ElympicsDisconnectedInfo>? Disconnected;

        event Action<MatchLauncherState>? StateChanged;
    }
}
