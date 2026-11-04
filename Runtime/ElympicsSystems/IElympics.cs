using System;
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

        #region Game start

        /// <summary>
        /// Confirms that this player is ready to play.
        /// Call it on every player client from a player action, such as a "Ready" or "Play" button. Bots are ready automatically.
        /// </summary>
        /// <remarks>
        /// When all players are ready, the server schedules the game start (<see cref="GameStartTick"/>) a few ticks ahead (see <see cref="ElympicsGameConfig.GameStartDelaySeconds"/>).
        /// If not all players are ready within <see cref="ElympicsGameConfig.ReadyTimeoutSeconds"/> from the first player connection,
        /// or no player connects within <see cref="ElympicsGameConfig.NoPlayerTimeoutSeconds"/>, the server ends the match with no result.
        /// Inputs for ticks before <see cref="GameStartTick"/>, and player-to-server RPCs sent before it, are ignored.
        /// Calling it again or on a spectator has no effect. On a server, it has an effect only in local modes, where it marks all players ready.
        /// </remarks>
        void MarkReady();

        /// <value>The tick the game starts at, or null while the start isn't scheduled (or not yet known to this client).</value>
        long? GameStartTick { get; }

        /// <value>True from <see cref="GameStartTick"/> on.</value>
        /// <remarks>Objects spawned after the start should check it, because <see cref="IGameStartHandler.OnGameStarted"/> has already been called.</remarks>
        bool IsGameStarted { get; }

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
        /// It's enabled automatically at the game start. Call <see cref="ElympicsScore.Enable"/> to customize it, or to set scores before the start.
        /// </remarks>
        ElympicsScore Score { get; }

        /// <value>The server's UTC time of the start tick, or null before the server has run it.</value>
        /// <remarks>
        /// Server-only. Clients and bots run ahead of the server and on their own clocks, so for the time since the start
        /// use ticks there: <c>(Tick - GameStartTick) * TickDuration</c>.
        /// </remarks>
        DateTime? GameStartUtc { get; }

        /// <param name="result">Results for all players. Ignored before the game start (no result is sent); if null after it, built from <see cref="Score"/>.</param>
        void EndGame(ResultMatchPlayerDatas result = null);

        #endregion
    }
}
