namespace Elympics
{
    public interface IInitializable : IObservable
    {
        /// <summary>
        /// Called during ElympicsBehaviour initialization just before gathering all its <see cref="ElympicsVar"/>s for enabling their synchronization.
        /// </summary>
        void Initialize() { }

        /// <summary>
        /// Called on client when initial state of an ElympicsBehaviour is received from server and all <see cref="ElympicsVar"/>s are already synchronized.
        /// </summary>
        /// <remarks>
        /// For objects that exist when client connects to a match this is called after the first snapshot received from the server is applied and before the first call to <see cref="IUpdatable.ElympicsUpdate"/>.
        /// For objects that are spawned during a match this is called after the first snapshot from server containing them is processed (before resimulation is performed, if necessary).
        /// </remarks>
        void InitializedByServer() { }
    }
}
