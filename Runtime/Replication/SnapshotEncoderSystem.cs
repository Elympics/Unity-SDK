using System.Collections.Generic;

namespace Elympics.Replication
{
    /// <summary>
    /// Builds the final per-player ElympicsSnapshot dictionaries from the scheduled
    /// dense-index lists. Each player receives a snapshot containing only the NetworkEntity
    /// state they are allowed to see, with their own input removed.
    /// This is the only pipeline system that allocates heap objects.
    /// Only iterates active players via the packed <paramref name="activePlayers"/> array.
    /// </summary>
    internal static class SnapshotEncoderSystem
    {
        /// <summary>
        /// Builds a per-player <see cref="ElympicsSnapshot"/> for each active player and
        /// writes it into <paramref name="outputSnapshots"/>.
        /// </summary>
        /// <param name="fullSnapshot">The complete server snapshot for the current tick containing all NetworkEntity states and input data (input).</param>
        /// <param name="playerIds">Array mapping player slot index to the <see cref="ElympicsPlayer"/> identifier used as the snapshot dictionary key (input).</param>
        /// <param name="activePlayers">Packed array of player slot indices currently active in the match; only entries <c>[0, activePlayers.Count)</c> are valid (input).</param>
        /// <param name="scheduled">Per-player 2-D buffer of dense indices produced by <see cref="BandwidthSchedulingSystem"/> indicating which NetworkEntities to include for each player (input).</param>
        /// <param name="denseToSparse">Array mapping dense index back to the sparse networkId; used to look up serialized state bytes in <paramref name="fullSnapshot"/> (input).</param>
        /// <param name="outputSnapshots">Dictionary keyed by <see cref="ElympicsPlayer"/> that receives the constructed per-player snapshot for each active player (output).</param>
        internal static void Execute(
            ElympicsSnapshot fullSnapshot,
            ElympicsPlayer[] playerIds,
            PackedArray<int> activePlayers,
            PackedArray2D<int> scheduled,
            int[] denseToSparse,
            Dictionary<ElympicsPlayer, ElympicsSnapshot> outputSnapshots)
        {
            if (fullSnapshot?.Data == null)
                return;

            for (var i = 0; i < activePlayers.Count; i++)
            {
                var p = activePlayers[i];
                var player = playerIds[p];
                var count = scheduled.RowCount(p);

                var playerSnapshot = new ElympicsSnapshot(
                    fullSnapshot.Tick,
                    fullSnapshot.TickStartUtc,
                    fullSnapshot.Factory,
                    new Dictionary<int, byte[]>(count),
                    fullSnapshot.TickToPlayersInputData != null
                        ? new Dictionary<int, TickToPlayerInput>(fullSnapshot.TickToPlayersInputData)
                        : null
                );

                _ = playerSnapshot.TickToPlayersInputData?.Remove((int)player);

                for (var e = 0; e < count; e++)
                {
                    var denseIndex = scheduled[p, e];
                    var networkId = denseToSparse[denseIndex];

                    if (playerSnapshot.Data != null && fullSnapshot.Data.TryGetValue(networkId, out var stateBytes))
                        playerSnapshot.Data[networkId] = stateBytes;
                }

                outputSnapshots[player] = playerSnapshot;
            }
        }
    }
}
