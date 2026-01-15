namespace Elympics
{
    internal class FactoryNetworkIdEnumerator
    {
        private readonly NetworkIdEnumerator _enumerator;

        public FactoryNetworkIdEnumerator(int startNetworkId, int endNetworkId) => _enumerator = NetworkIdEnumerator.CreateNetworkIdEnumerator(startNetworkId, endNetworkId);

        public bool Equals(FactoryPartState historyPartState, FactoryPartState receivedPartState, ElympicsPlayer player, long historyTick, long lastSimulatedTick)
        {
            var areCurrentNetworkIdsEqual = historyPartState.currentNetworkId == receivedPartState.currentNetworkId;

            if (!areCurrentNetworkIdsEqual)
            {
                ElympicsLogger.LogWarning($"The predicted ID of the last object spawned for player {player} in local snapshot history for tick {historyTick} " +
                    $"doesn't match that received from the game server. " +
                    $"ID in local history: {historyPartState.currentNetworkId} received ID: {receivedPartState.currentNetworkId}. " +
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
