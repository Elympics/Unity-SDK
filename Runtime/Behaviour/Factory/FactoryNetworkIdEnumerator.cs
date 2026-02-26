namespace Elympics
{
    internal class FactoryNetworkIdEnumerator
    {
        private readonly NetworkIdEnumerator _enumerator;

        public FactoryNetworkIdEnumerator(int playerIndex)
            : this(playerIndex,
                   ElympicsBehavioursManager.IndicesPerPlayer,
                   ElympicsBehavioursManager.SceneObjectsMaxIndex,
                   ElympicsBehavioursManager.SpawnableSpecialPlayerIndices)
        {
        }

        /// <summary>
        /// Internal constructor used by the public constructor and overload entry-points.
        /// Accepts explicit allocation parameters instead of reading from ElympicsBehavioursManager statics.
        /// </summary>
        private FactoryNetworkIdEnumerator(int playerIndex, int indicesPerPlayer, int sceneObjectsMaxIndex, int[] spawnableSpecialPlayerIndices) => _enumerator = NetworkIdEnumerator.CreateForPlayer(playerIndex, indicesPerPlayer, sceneObjectsMaxIndex, spawnableSpecialPlayerIndices);

        public bool Equals(FactoryPartState historyPartState, FactoryPartState receivedPartState, ElympicsPlayer player, long historyTick, long lastSimulatedTick)
        {
            var areCurrentNetworkIdsEqual = historyPartState.CurrentNetworkId == receivedPartState.CurrentNetworkId;

            if (!areCurrentNetworkIdsEqual)
            {
                ElympicsLogger.LogWarning($"The current enumerator position (last allocated NetworkId) for player {player} in local snapshot history for tick {historyTick} " +
                    $"doesn't match that received from the game server. " +
                    $"Position in local history: {historyPartState.CurrentNetworkId}, received position: {receivedPartState.CurrentNetworkId}. " +
                    $"Last simulated tick: {lastSimulatedTick}. " +
                    $"This means that the client incorrectly predicted spawning/destruction of objects.");
            }

            return areCurrentNetworkIdsEqual;
        }

        public int GetCurrent() => _enumerator.GetCurrent();
        public void MoveTo(int to) => _enumerator.MoveTo(to);
        public int MoveNextAndGetCurrent() => _enumerator.MoveNextAndGetCurrent();
        public void ReleaseId(int networkId) => _enumerator.ReleaseId(networkId);

        /// <inheritdoc cref="NetworkIdEnumerator.SyncAllocatedId"/>
        internal void SyncAllocatedId(int networkId) => _enumerator.SyncAllocatedId(networkId);
    }
}
