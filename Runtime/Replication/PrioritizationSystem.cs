namespace Elympics.Replication
{
    /// <summary>
    /// Filters the interest-managed NetworkEntity list using ack-aware two-condition checks.
    /// A NetworkEntity is included for a player if any of these conditions hold:
    /// <list type="bullet">
    /// <item>Cold start (player has never received a snapshot)</item>
    /// <item>Genuinely dirty (NetworkEntity changed after last send to this player)</item>
    /// <item>Unacked re-send (sent but not confirmed, and throttle interval has elapsed)</item>
    /// </list>
    /// NetworkEntities that have been sent AND acknowledged are skipped (confirmed delivered).
    /// Operates entirely on dense indices — no sparse lookups.
    /// Only iterates active players via the packed <paramref name="activePlayers"/> array.
    /// </summary>
    internal static class PrioritizationSystem
    {
        /// <summary>
        /// Filters each player's relevant-NetworkEntity list down to NetworkEntities that genuinely need
        /// to be sent this tick and writes the results into <paramref name="dirtySorted"/>.
        /// </summary>
        /// <param name="playerLastReceivedSnapshot">Per-player array of the last snapshot tick acknowledged by each player; negative means the player has never received a snapshot (input).</param>
        /// <param name="activePlayers">Packed array of player slot indices currently active in the match; only entries <c>[0, activePlayers.Count)</c> are valid (input).</param>
        /// <param name="lastModifiedTick">Array (indexed by dense index) recording the tick on which each NetworkEntity's state last changed (input).</param>
        /// <param name="lastSentTick">Per-player-per-NetworkEntity 2-D array (<c>[playerIndex, denseIndex]</c>) recording the last tick on which each NetworkEntity was included in a snapshot for that player (input).</param>
        /// <param name="currentTick">The tick number being processed; used to compute re-send throttle intervals.</param>
        /// <param name="netUpdateInterval">Array (indexed by dense index) of per-NetworkEntity minimum tick intervals between unacknowledged re-sends (input).</param>
        /// <param name="relevantEntities">Per-player 2-D buffer of dense indices produced by <see cref="InterestManagementSystem"/> (input).</param>
        /// <param name="dirtySorted">Per-player 2-D buffer filled with the dense indices of NetworkEntities that must be sent this tick (output).</param>
        internal static void Execute(
            long[] playerLastReceivedSnapshot,
            PackedArray<int> activePlayers,
            long[] lastModifiedTick,
            long[][] lastSentTick,
            long currentTick,
            int[] netUpdateInterval,
            PackedArray2D<int> relevantEntities,
            PackedArray2D<int> dirtySorted)
        {
            for (var i = 0; i < activePlayers.Count; i++)
            {
                var activePlayerIndex = activePlayers[i];
                var lastRecv = playerLastReceivedSnapshot[activePlayerIndex];
                var relevantCount = relevantEntities.RowCount(activePlayerIndex);

                // Cold start: player has never received any snapshot — include everything
                if (lastRecv < 0)
                {
                    for (var j = 0; j < relevantCount; j++)
                        dirtySorted.Append(activePlayerIndex, relevantEntities[activePlayerIndex, j]);
                    continue;
                }

                for (var j = 0; j < relevantCount; j++)
                {
                    var networkEntityDenseIndex = relevantEntities[activePlayerIndex, j];

                    var sentSinceChange = lastSentTick[activePlayerIndex][networkEntityDenseIndex] >= lastModifiedTick[networkEntityDenseIndex];
                    var clientAckedSend = lastRecv >= lastSentTick[activePlayerIndex][networkEntityDenseIndex];

                    // Confirmed delivered: sent after last change AND client acked that send
                    if (sentSinceChange && clientAckedSend)
                        continue;

                    // Genuinely dirty: entity changed after our last send — always include
                    if (!sentSinceChange)
                    {
                        dirtySorted.Append(activePlayerIndex, networkEntityDenseIndex);
                        continue;
                    }

                    // Sent after last change but client has not acked that send — throttled re-send
                    if (currentTick - lastSentTick[activePlayerIndex][networkEntityDenseIndex] >= netUpdateInterval[networkEntityDenseIndex])
                        dirtySorted.Append(activePlayerIndex, networkEntityDenseIndex);
                }
            }
        }
    }
}
