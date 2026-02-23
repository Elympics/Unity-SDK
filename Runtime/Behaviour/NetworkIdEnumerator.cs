using System;
using System.Collections.Generic;

namespace Elympics
{
    /// <summary>
    /// Manages NetworkId allocation with generational IDs for stale reference detection.
    /// Uses FIFO index reuse to spread generation increments evenly across all indices,
    /// preventing generation wrap-around in long matches (up to 24 hours).
    /// This is a runtime-only allocator for dynamic object spawns. Scene object IDs
    /// are managed separately by SceneNetworkIdAssigner (editor-only).
    /// </summary>
    public class NetworkIdEnumerator
    {
        private const int InitialGenerationsCapacity = 256;

        private readonly int _min;
        private readonly int _max;
        private int _current;
        private readonly HashSet<int> _dynamicAllocatedIds;

        // Generational ID tracking - FIFO queue to spread generation increments across all indices
        private readonly Queue<int> _freeIndices;

        // Companion set for O(1) membership test — prevents double-enqueue in ReleaseId
        private readonly HashSet<int> _freeIndicesSet;
        private ushort[] _generations;
        private int _nextFreshIndex;

        // Delegate static methods to NetworkIdConstants for consistency
        internal static int ExtractIndex(int networkId) => NetworkIdConstants.ExtractIndex(networkId);
        internal static int ExtractGeneration(int networkId) => NetworkIdConstants.ExtractGeneration(networkId);
        internal static int EncodeNetworkId(int generation, int index) => NetworkIdConstants.EncodeNetworkId(generation, index);

        /// <summary>
        /// Checks if a NetworkId is still valid (not stale).
        /// Returns true for forced IDs (generation 0) and active generational IDs.
        /// </summary>
        public bool IsValid(int networkId)
        {
            var index = ExtractIndex(networkId);
            var generation = ExtractGeneration(networkId);

            // Generation 0 = scene/forced ID, always considered valid
            if (generation == 0)
                return true;

            if (index >= _generations.Length)
                return false;

            return _generations[index] == generation;
        }

        /// <summary>
        /// Creates a NetworkIdEnumerator for a specific player's allocation range.
        /// Slot assignment:
        /// - Special spawnable players (All, World, etc.) get slots based on their position in spawnableSpecialPlayerIndices
        /// - Regular players (0, 1, 2, ...) get slots starting at spawnableSpecialPlayerIndices.Length
        /// </summary>
        public static NetworkIdEnumerator CreateForPlayer(int playerIndex, int indicesPerPlayer, int sceneObjectsMaxIndex, int[] spawnableSpecialPlayerIndices)
        {
            var startIndex = NetworkIdConstants.GetStartIndexForPlayer(playerIndex, sceneObjectsMaxIndex, indicesPerPlayer, spawnableSpecialPlayerIndices);
            var endIndex = NetworkIdConstants.GetEndIndexForPlayer(playerIndex, sceneObjectsMaxIndex, indicesPerPlayer, spawnableSpecialPlayerIndices);
            return new NetworkIdEnumerator(startIndex, endIndex);
        }

        /// <summary>
        /// Creates a NetworkIdEnumerator with explicit min/max range. For testing purposes.
        /// </summary>
        public static NetworkIdEnumerator CreateWithRange(int min, int max)
        {
            return new NetworkIdEnumerator(min, max);
        }

        private NetworkIdEnumerator(int min, int max)
        {
            _min = min;
            _max = Math.Min(max, NetworkIdConstants.MaxIndex);
            _freeIndices = new Queue<int>();
            _freeIndicesSet = new HashSet<int>(InitialGenerationsCapacity);
            _generations = new ushort[InitialGenerationsCapacity];
            _dynamicAllocatedIds = new HashSet<int>();
            _current = _min - 1; // Immediately overwritten by MoveNextAndGetCurrent below
            _nextFreshIndex = _min;

            _ = MoveNextAndGetCurrent();
        }

        public int GetCurrent() => _current;

        private int GetNext()
        {
            int index;
            int generation;

            // Try to reuse from free queue first (FIFO - oldest released index).
            // FIFO ensures indices cycle through all free slots before reusing,
            // spreading generation increments evenly to prevent wrap-around in long matches.
            //
            // Indices whose current-generation id is still live in _dynamicAllocatedIds (e.g.
            // reclaimed by SyncAllocatedId while sitting in the queue) are simply discarded —
            // NOT re-enqueued. This is safe because SyncAllocatedId's caller will call ReleaseId
            // later, which will add the index back to the queue at that time. The companion
            // _freeIndicesSet prevents double-enqueue so the queue length always equals the
            // number of distinct free indices.
            //
            // A second subtle case arises from ReleaseId → SyncAllocatedId → ReleaseId on the same
            // index: two physical entries end up in the queue because SyncAllocatedId clears
            // _freeIndicesSet (allowing the second ReleaseId to enqueue again). This is safe because
            // the first (stale) entry is consumed by GetNext — either allocated normally or discarded
            // when the live entry has already incremented the generation — and the second entry is
            // processed correctly in a subsequent GetNext call.
            while (_freeIndices.Count > 0)
            {
                index = _freeIndices.Dequeue();
                _ = _freeIndicesSet.Remove(index);

                // Discard if SyncAllocatedId re-registered this index while it was in the queue.
                var currGen = _generations[index];
                var currentId = EncodeNetworkId(currGen, index);
                if (_dynamicAllocatedIds.Contains(currentId))
                    continue;

                // Increment generation on reuse
                if (currGen == ushort.MaxValue)
                    throw ElympicsLogger.LogException(new OverflowException($"Cannot use new networkId generation. The pool of generations (min: {0}, max: {ushort.MaxValue}) has been used up."));
                generation = currGen + 1;

                var candidateId = EncodeNetworkId(generation, index);
                _generations[index] = (ushort)generation;
                if (!_dynamicAllocatedIds.Add(candidateId))
                    throw ElympicsLogger.LogException($"Generated network ID {candidateId} (index={index}, gen={generation}) is already in use.");

                return candidateId;
            }

            // Allocate new fresh index
            index = GetNextFreshIndex();
            EnsureGenerationCapacity(index);
            _generations[index] = 1; // First generation for dynamic objects
            generation = 1;

            var networkId = EncodeNetworkId(generation, index);

            if (!_dynamicAllocatedIds.Add(networkId))
                throw ElympicsLogger.LogException($"Generated network ID {networkId} (index={index}, gen={generation}) is already in use.");

            return networkId;
        }

        private int GetNextFreshIndex()
        {
            var index = _nextFreshIndex;
            var isOverflow = false;

            while (true)
            {
                if (index > _max)
                {
                    if (!isOverflow)
                    {
                        isOverflow = true;
                        index = _min;
                        continue;
                    }

                    throw ElympicsLogger.LogException(new OverflowException("Cannot generate a network ID. "
                                                                            + $"The pool of indices between min: {_min} and max: {_max} has been used up."));
                }

                // Skip already-used indices
                if (index < _generations.Length && _generations[index] > 0)
                {
                    index++;
                    continue;
                }

                break;
            }

            _nextFreshIndex = index + 1;
            return index;
        }

        public void ReleaseId(int networkId)
        {
            _ = _dynamicAllocatedIds.Remove(networkId);

            var index = ExtractIndex(networkId);
            var generation = ExtractGeneration(networkId);

            // Only recycle dynamic IDs (generation > 0) that match current generation.
            // Scene objects (generation 0) are never recycled.
            if (generation == 0)
                return;

            if (index >= _generations.Length || _generations[index] != generation)
            {
                ElympicsLogger.LogWarning($"Attempted to release stale or invalid NetworkId {networkId} (index={index}, gen={generation})");
                return;
            }

            // Guard against double-release: if the index is already in the free queue, drop the
            // second release silently. Without this guard a double-release creates a ghost entry
            // that causes GetNext to hand out the same index twice in future ticks.
            if (_freeIndicesSet.Contains(index))
                return;

            // Add to back of free queue (FIFO - will be reused after all other free indices)
            _freeIndices.Enqueue(index);
            _ = _freeIndicesSet.Add(index);
        }

        public int MoveNextAndGetCurrent()
        {
            _current = GetNext();
            return _current;
        }

        public void MoveTo(int newCurrent) => _current = newCurrent;

        /// <summary>
        /// Informs the enumerator that a NetworkId has been allocated externally (e.g. assigned by the server
        /// and received in a snapshot). This keeps the enumerator state consistent so future allocations
        /// via <see cref="MoveNextAndGetCurrent"/> do not collide with ids already in use.
        /// Silently ignores generation-0 (scene) ids and ids whose index is outside this enumerator's range.
        /// </summary>
        internal void SyncAllocatedId(int networkId)
        {
            var index = ExtractIndex(networkId);
            var generation = ExtractGeneration(networkId);

            // Scene objects (generation 0) are never managed by this allocator
            if (generation == 0)
                return;

            // Ignore ids outside our assigned index range
            if (index < _min || index > _max)
                return;

            EnsureGenerationCapacity(index);

            _generations[index] = (ushort)generation;

            // Advance _nextFreshIndex past this index so fresh allocation never picks it
            if (index >= _nextFreshIndex)
                _nextFreshIndex = index + 1;

            // If this index was in the free queue (from a prior ReleaseId), remove it from the
            // companion set so that a future ReleaseId for this synced id is not incorrectly
            // treated as a double-release.
            _ = _freeIndicesSet.Remove(index);

            // Track in the allocated set so double-allocation is detected
            _ = _dynamicAllocatedIds.Add(networkId);
        }

        private void EnsureGenerationCapacity(int index)
        {
            if (index < _generations.Length)
                return;

            var newCapacity = Math.Max(_generations.Length * 2, index + 1);
            newCapacity = Math.Min(newCapacity, NetworkIdConstants.MaxIndex + 1);
            var newGenerations = new ushort[newCapacity];
            Array.Copy(_generations, newGenerations, _generations.Length);
            _generations = newGenerations;
        }
    }
}
