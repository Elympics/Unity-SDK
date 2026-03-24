namespace Elympics.Replication
{
    /// <summary>
    /// Dummy System. Will be refined in further iterations.
    /// Copies every prioritized dirty NetworkEntity into the Scheduled buffer, making all dirty
    /// NetworkEntities eligible for sending this tick. Only iterates active players via the
    /// packed <paramref name="activePlayers"/> array.
    /// </summary>
    internal static class BandwidthSchedulingSystem
    {
        /// <summary>
        /// Populates <paramref name="scheduled"/> with the dense indices of NetworkEntities to send
        /// this tick, derived from the prioritized dirty list.
        /// </summary>
        /// <param name="activePlayers">Packed array of player slot indices currently active in the match; only entries <c>[0, activePlayers.Count)</c> are valid (input).</param>
        /// <param name="dirtySorted">Per-player 2-D buffer of dense indices produced by <see cref="PrioritizationSystem"/> (input).</param>
        /// <param name="scheduled">Per-player 2-D buffer filled with the dense indices of NetworkEntities that will be included in this tick's snapshot for each player (output).</param>
        internal static void Execute(
            PackedArray<int> activePlayers,
            PackedArray2D<int> dirtySorted,
            PackedArray2D<int> scheduled)
        {
            for (var i = 0; i < activePlayers.Count; i++)
            {
                var p = activePlayers[i];
                var count = dirtySorted.RowCount(p);

                for (var e = 0; e < count; e++)
                    scheduled.Append(p, dirtySorted[p, e]);
            }
        }
    }
}
