namespace Elympics
{
    /// <summary>Receives the match start on the server and on player clients. See <see cref="IElympics.MarkReady"/>.</summary>
    public interface IMatchStartHandler : IObservable
    {
        /// <summary>
        /// Called once, on the first tick from <see cref="IElympics.StartTick"/> on, before <see cref="IUpdatable.ElympicsUpdate"/>.
        /// </summary>
        /// <remarks>
        /// The server calls it exactly at <paramref name="startTick"/>. A client that learns about the start late calls it on its first predicted tick after that.
        /// If it changes synchronized state, it must do so the same way on the server and on clients, so that prediction stays correct.
        /// It isn't called for objects spawned after the start, so check <see cref="IElympics.IsMatchStarted"/> there.
        /// Inputs, <see cref="ElympicsMonoBehaviour.ElympicsInstantiate"/> and <see cref="ElympicsMonoBehaviour.ElympicsDestroy"/> aren't available here; use <see cref="IUpdatable.ElympicsUpdate"/> for them.
        /// </remarks>
        /// <param name="startTick">The tick the match started at.</param>
        void OnMatchStarted(long startTick);
    }
}
