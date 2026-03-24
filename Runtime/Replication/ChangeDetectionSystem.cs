using System;
using System.Collections.Generic;

namespace Elympics.Replication
{
    /// <summary>
    /// Compares per-NetworkEntity serialized state between the current and previous snapshot.
    /// When a difference is detected (or no previous data exists), the NetworkEntity's
    /// LastModifiedTick is stamped with the current tick so downstream systems
    /// know which NetworkEntities carry new information.
    /// </summary>
    internal static class ChangeDetectionSystem
    {
        /// <summary>
        /// Compares serialized NetworkEntity state between the current and previous snapshot and stamps
        /// <paramref name="lastModifiedTick"/> for any NetworkEntity whose bytes have changed.
        /// </summary>
        /// <param name="currentData">Serialized state of all NetworkEntities in the current snapshot, keyed by sparse networkId.</param>
        /// <param name="previousData">Serialized state of all NetworkEntities in the previous snapshot, keyed by sparse networkId. May be null on the first tick.</param>
        /// <param name="currentTick">The tick number being processed; written into <paramref name="lastModifiedTick"/> for changed NetworkEntities.</param>
        /// <param name="lastModifiedTick">Array (indexed by dense index) that records the last tick on which each NetworkEntity's state changed; updated in-place (output).</param>
        /// <param name="sparseToDense">Array mapping sparse networkId slot index to dense index; negative value means the NetworkEntity is not registered.</param>
        internal static void Execute(
            Dictionary<int, byte[]> currentData,
            Dictionary<int, byte[]> previousData,
            long currentTick,
            long[] lastModifiedTick,
            int[] sparseToDense)
        {

            if (currentData == null)
                return;

            foreach (var (networkId, currentBytes) in currentData)
            {
                var sparseIndex = ElympicsWorld.ExtractIndex(networkId);
                var denseIndex = sparseToDense[sparseIndex];
                if (denseIndex < 0)
                    continue;

                bool changed;

                if (previousData == null || !previousData.TryGetValue(networkId, out var previousBytes))
                    changed = true;
                else
                    changed = (currentBytes, previousBytes) switch
                    {
                        (null, null) => false,
                        (null, _) => true,
                        (_, null) => true,
                        _ => !currentBytes.AsSpan().SequenceEqual(previousBytes.AsSpan()),
                    };

                if (changed)
                {
                    lastModifiedTick[denseIndex] = currentTick;
                }
            }
        }
    }
}
