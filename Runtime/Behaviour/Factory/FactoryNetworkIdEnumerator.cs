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
        /// Constructor with explicit parameters for testing.
        /// </summary>
        private FactoryNetworkIdEnumerator(int playerIndex, int indicesPerPlayer, int sceneObjectsMaxIndex, int[] spawnableSpecialPlayerIndices) => _enumerator = NetworkIdEnumerator.CreateForPlayer(playerIndex, indicesPerPlayer, sceneObjectsMaxIndex, spawnableSpecialPlayerIndices);

        public bool Equals(FactoryPartState historyPartState, FactoryPartState receivedPartState, ElympicsPlayer player, long historyTick, long lastSimulatedTick)
        {
            var areCurrentNetworkIdsEqual = historyPartState.CurrentNetworkId == receivedPartState.CurrentNetworkId;

            if (!areCurrentNetworkIdsEqual)
            {
                ElympicsLogger.LogWarning($"The predicted ID of the last object spawned for player {player} in local snapshot history for tick {historyTick} " +
                    $"doesn't match that received from the game server. " +
                    $"ID in local history: {historyPartState.CurrentNetworkId} received ID: {receivedPartState.CurrentNetworkId}. " +
                    $"Last simulated tick: {lastSimulatedTick}. " +
                    $"This means that the client incorrectly predicted spawning/destruction of objects.");
            }

            return areCurrentNetworkIdsEqual;
        }

        public int GetCurrent() => _enumerator.GetCurrent();
        public void MoveTo(int to) => _enumerator.MoveTo(to);
        public int MoveNextAndGetCurrent() => _enumerator.MoveNextAndGetCurrent();
        public void ReleaseId(int networkId) => _enumerator.ReleaseId(networkId);
    }
}
