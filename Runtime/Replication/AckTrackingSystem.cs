namespace Elympics.Replication
{
    /// <summary>
    /// Records the current tick as the last-sent tick for every NetworkEntity included in this
    /// tick's Scheduled buffer. Only NetworkEntities that passed through bandwidth scheduling are
    /// stamped, so <see cref="PrioritizationSystem"/> can accurately distinguish confirmed
    /// deliveries from unacknowledged sends on subsequent ticks.
    /// Runs after <see cref="SnapshotEncoderSystem"/> as the final pipeline stage.
    /// </summary>
    internal static class AckTrackingSystem
    {
        /// <summary>
        /// Stamps <paramref name="lastSentTick"/> with <paramref name="currentTick"/> for every
        /// NetworkEntity scheduled for each active player this tick.
        /// </summary>
        /// <param name="activePlayers">Packed array of player slot indices currently active in the match; only entries <c>[0, activePlayers.Count)</c> are valid (input).</param>
        /// <param name="scheduled">Per-player 2-D buffer of dense indices produced by <see cref="BandwidthSchedulingSystem"/> listing NetworkEntities included in this tick's snapshot for each player (input).</param>
        /// <param name="lastSentTick">Per-player-per-NetworkEntity 2-D array (<c>[playerIndex, denseIndex]</c>) recording the last tick on which each NetworkEntity was sent to each player; updated in-place (output).</param>
        /// <param name="currentTick">The tick number being processed; written into <paramref name="lastSentTick"/> for every scheduled NetworkEntity.</param>
        internal static void Execute(
            PackedArray<int> activePlayers,
            PackedArray2D<int> scheduled,
            long[][] lastSentTick,
            long currentTick)
        {
            for (var i = 0; i < activePlayers.Count; i++)
            {
                var p = activePlayers[i];
                var count = scheduled.RowCount(p);
                for (var e = 0; e < count; e++)
                    lastSentTick[p][scheduled[p, e]] = currentTick;
            }
        }
    }
}
