using System.Collections.Generic;

namespace Elympics.Replication
{
    /// <summary>
    /// Determines which NetworkEntities are relevant (visible) to each player using a per-NetworkEntity
    /// bitmask. Each bit in the mask corresponds to a player index. The system writes the
    /// dense indices of visible NetworkEntities into a per-player output buffer.
    /// </summary>
    internal static class InterestManagementSystem
    {
        /// <summary>
        /// Populates <paramref name="relevantEntities"/> with the dense indices of NetworkEntities
        /// that are visible to each active player this tick.
        /// </summary>
        /// <param name="currentData">Serialized state of all NetworkEntities in the current snapshot, keyed by sparse networkId; iterated to enumerate registered NetworkEntities.</param>
        /// <param name="interestMask">Array (indexed by dense index) of per-NetworkEntity visibility bitmasks; bit <c>p</c> set means the NetworkEntity is visible to player index <c>p</c> (input).</param>
        /// <param name="activePlayers">Packed array of player slot indices currently active in the match; only entries <c>[0, activePlayers.Count)</c> are valid (input).</param>
        /// <param name="sparseToDense">Array mapping sparse networkId slot index to dense index; negative value means the NetworkEntity is not registered (input).</param>
        /// <param name="relevantEntities">Per-player 2-D buffer filled with dense indices of NetworkEntities relevant to each player (output).</param>
        internal static void Execute(
            Dictionary<int, byte[]> currentData,
            uint[] interestMask,
            PackedArray<int> activePlayers,
            int[] sparseToDense,
            PackedArray2D<int> relevantEntities)
        {
            if (currentData == null)
                return;

            foreach (var kvp in currentData)
            {
                var networkId = kvp.Key;
                var sparseIndex = ElympicsWorld.ExtractIndex(networkId);
                var denseIndex = sparseToDense[sparseIndex];
                if (denseIndex < 0)
                    continue;

                var mask = interestMask[denseIndex];

                for (var i = 0; i < activePlayers.Count; i++)
                {
                    var p = activePlayers[i];
                    if ((mask & (1u << p)) == 0)
                        continue;
                    relevantEntities.Append(p, denseIndex);
                }
            }
        }

        /// <summary>
        /// Converts an ElympicsPlayer visibility setting to an uint bitmask.
        /// All sets every player bit, World sets none, a regular player sets its own bit.
        /// </summary>
        internal static uint ConvertVisibleFor(ElympicsPlayer visibleFor, int maxPlayers)
        {
            if (visibleFor == ElympicsPlayer.All)
                return AllPlayersMask(maxPlayers);

            if (visibleFor == ElympicsPlayer.World)
                return 0u;

            var playerIndex = (int)visibleFor;
            if (playerIndex < 0)
                return 0u;

            return 1u << playerIndex;
        }

        private static uint AllPlayersMask(int maxPlayers) => maxPlayers >= 32 ? uint.MaxValue : (1u << maxPlayers) - 1;
    }
}
