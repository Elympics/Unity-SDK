using System.Threading;
using Elympics.Core;
using Elympics.Cysharp.Threading.Tasks;

namespace Elympics
{
    public interface IElympics
    {
        /// <value>Current player identifier.</value>
        /// <remarks>If client or bot is handled in server (e.g. in "Local Player And Bots" mode), their ID is only available during input gathering and game
        /// initialization.</remarks>
        ElympicsPlayer Player { get; }

        /// <value>Is game instance a server?</value>
        /// <remarks>If client or bot is handled in server (e.g. in "Local Player And Bots" mode), the property is always truthy for them.</remarks>
        bool IsServer { get; }

        /// <value>Is game instance a client?</value>
        /// <remarks>If client is handled in server (in "Local Player And Bots" mode), the property is only meaningful during input gathering and game
        /// initialization.</remarks>
        bool IsClient { get; }

        /// <value>Is game instance a bot?</value>
        /// <remarks>If bot is handled in server (in "Local Player And Bots" mode or with "Bots inside server" option checked), the property is only meaningful
        /// during input gathering and game initialization.</remarks>
        bool IsBot { get; }

        /// <value>Shortcut for (<see cref="IsClient"/> || <see cref="IsBot"/>).</value>
        bool IsClientOrBot { get; }

        /// <value>The interval in seconds at which network synchronization occurs. It is equal to <see cref="UnityEngine.Time.fixedDeltaTime"/> and calculated as 1/<see cref="TicksPerSecond"/>.</value>
        float TickDuration { get; }

        /// <value>The total number of ticks per seconds.</value>
        /// <seealso cref="TickDuration"/>
        int TicksPerSecond { get; }

        /// <value>Number of current tick</value>
        long Tick { get; }

        bool TryGetBehaviour(int networkId, out ElympicsBehaviour elympicsBehaviour);

        #region Match start

        /// <summary>
        /// Confirms that the game launched successfully for this player.
        /// Call it on every player client once the game is playable. Bots are ready automatically.
        /// </summary>
        /// <remarks>
        /// When all players are ready, the server schedules the match start (<see cref="StartTick"/>) shortly ahead.
        /// If not all players are ready within <see cref="ElympicsGameConfig.ReadyTimeoutSeconds"/>, the server ends the match with a null result (refunded).
        /// Inputs for ticks before <see cref="StartTick"/> are ignored.
        /// Calling it again, or on a server or spectator, has no effect.
        /// </remarks>
        void MarkReady();

        /// <value>The tick the match starts at, or null while the start isn't scheduled (or not yet known to this client).</value>
        long? StartTick { get; }

        /// <value>True from <see cref="StartTick"/> on.</value>
        /// <remarks>Objects spawned after the start should check it, because <see cref="IMatchStartHandler.OnMatchStarted"/> has already been called.</remarks>
        bool IsMatchStarted { get; }

        #endregion

        #region Client

        UniTask ConnectAndJoinAsPlayerAsync(CancellationToken ct = default);
        UniTask ConnectAndJoinAsSpectatorAsync(CancellationToken ct = default);
        void Disconnect();

        #endregion

        #region Server

        /// <summary>
        /// Server-only live tracker of the current score.
        /// </summary>
        /// <remarks>
        /// <see cref="ElympicsScore.Enable"/> must be called before setting/retrieving any score.
        /// </remarks>
        ElympicsScore Score { get; }

        /// <summary>Ends the match.</summary>
        /// <param name="result">Results for all players. Ignored (a null result is sent) before the match start; if null after it, built from <see cref="Score"/>.</param>
        void EndGame(ResultMatchPlayerDatas result = null);

        #endregion
    }
}
