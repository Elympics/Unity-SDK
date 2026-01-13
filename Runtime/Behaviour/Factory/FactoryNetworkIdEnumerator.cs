using System.IO;

namespace Elympics
{
    internal class FactoryNetworkIdEnumerator
    {
        private readonly NetworkIdEnumerator _enumerator;

        public FactoryNetworkIdEnumerator(int startNetworkId, int endNetworkId) => _enumerator = NetworkIdEnumerator.CreateNetworkIdEnumerator(startNetworkId, endNetworkId);

        public void Serialize(BinaryWriter bw) => bw.Write(_enumerator.GetCurrent());

        public void Deserialize(BinaryReader br) => _enumerator.MoveTo(br.ReadInt32());

        public bool Equals(BinaryReader historyStateReader, BinaryReader receivedStateReader, ElympicsPlayer player, long historyTick, long lastSimulatedTick)
        {
            var historyCurrentNetworkId = historyStateReader.ReadInt32();
            var receivedCurrentNetworkId = receivedStateReader.ReadInt32();
            var areCurrentNetworkIdsEqual = historyCurrentNetworkId == receivedCurrentNetworkId;

            if (!areCurrentNetworkIdsEqual)
            {
                ElympicsLogger.LogWarning($"The predicted ID of the last object spawned for player {player} in local snapshot history for tick {historyTick} " +
                    $"doesn't match that received from the game server. " +
                    $"ID in local history: {historyCurrentNetworkId} received ID: {receivedCurrentNetworkId}. " +
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
