namespace Elympics
{
    /// <summary>Receives the game start on the server and on player clients. See <see cref="IElympics.MarkReady"/>.</summary>
    public interface IGameStartHandler : IObservable
    {
        /// <summary>
        /// Called once, on the first tick from <see cref="IElympics.GameStartTick"/> on, before <see cref="IUpdatable.ElympicsUpdate"/>.
        /// </summary>
        /// <remarks>
        /// The server calls it exactly at <paramref name="startTick"/>. A client that learns about the start late calls it on its first predicted tick after that.
        /// Like <see cref="IServerHandlerGuid.OnServerInit"/> and other one-time calls, it isn't called again when a client resimulates across the start,
        /// so predicted state set here is wrong until a server snapshot from the start on arrives.
        /// For predicted gameplay, check <see cref="IElympics.IsGameStarted"/> in <see cref="IUpdatable.ElympicsUpdate"/> instead.
        /// It isn't called for objects spawned after the start, so check <see cref="IElympics.IsGameStarted"/> there.
        /// Inputs, <see cref="ElympicsMonoBehaviour.ElympicsInstantiate"/> and <see cref="ElympicsMonoBehaviour.ElympicsDestroy"/> aren't available here; use <see cref="IUpdatable.ElympicsUpdate"/> for them.
        /// </remarks>
        /// <param name="startTick">The tick the game started at.</param>
        void OnGameStarted(long startTick);
    }
}
