using System.IO;

namespace Elympics
{
    internal class FactoryNetworkIdEnumerator
    {
        private readonly NetworkIdEnumerator _enumerator;

        public FactoryNetworkIdEnumerator(int startNetworkId, int endNetworkId) => _enumerator = NetworkIdEnumerator.CreateNetworkIdEnumerator(startNetworkId, endNetworkId);

        public void Serialize(BinaryWriter bw) => bw.Write(_enumerator.GetCurrent());

        public void Deserialize(BinaryReader br) => _enumerator.MoveTo(br.ReadInt32());

        public bool Equals(BinaryReader historyStateReader, BinaryReader receivedStateReader, ElympicsPlayer player)
        {
            var historyCurrentNetworkId = historyStateReader.ReadInt32();
            var receivedCurrentNetworkId = receivedStateReader.ReadInt32();
            var areCurrentNetworkIdsEqual = historyCurrentNetworkId == receivedCurrentNetworkId;

            if (!areCurrentNetworkIdsEqual)
            {
                ElympicsLogger.LogWarning($"The predicted ID of the last object spawned for player {player} in local snapshot history " +
                    $"doesn't match that received from the game server. " +
                    $"Local history: {historyCurrentNetworkId} received: {receivedCurrentNetworkId}. " +
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
