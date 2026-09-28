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

        /// <summary>
        /// Loads the gameplay scene of the requested game and connects. Completes when the match is joined or the launch fails.
        /// Accepted only in <see cref="MatchLauncherState.Idle"/>, otherwise fails with <see cref="ElympicsLaunchErrorCode.Busy"/>.
        /// </summary>
        /// <param name="ct">Stops waiting for the result. Does not cancel the launch — use <see cref="AbortMatchAsync"/> for that.</param>
        UniTask<ElympicsLaunchResult> LaunchMatchAsync(ElympicsLaunchRequest request, CancellationToken ct = default);

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
