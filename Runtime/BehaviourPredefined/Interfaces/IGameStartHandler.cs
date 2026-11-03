namespace Elympics
{
    /// <summary>Receives the game start on the server and on player clients, for visuals only. See <see cref="IElympics.MarkReady"/>.</summary>
    public interface IGameStartHandler : IObservable
    {
        /// <summary>
        /// Called once, on the first tick from <see cref="IElympics.GameStartTick"/> on, before <see cref="IUpdatable.ElympicsUpdate"/>.
        /// </summary>
        /// <remarks>
        /// Use it for visuals only. Start gameplay by checking <see cref="IElympics.IsGameStarted"/> in <see cref="IUpdatable.ElympicsUpdate"/>:
        /// this call isn't repeated when a client resimulates across the start, and isn't made for objects spawned after the start.
        /// The server calls it exactly at <paramref name="startTick"/>. A client that learns about the start late calls it on its first predicted tick after that.
        /// Inputs, <see cref="ElympicsMonoBehaviour.ElympicsInstantiate"/> and <see cref="ElympicsMonoBehaviour.ElympicsDestroy"/> aren't available here.
        /// </remarks>
        /// <param name="startTick">The tick the game started at.</param>
        void OnGameStarted(long startTick);
    }
}
