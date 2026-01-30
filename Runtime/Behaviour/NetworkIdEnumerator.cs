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
            _generations = new ushort[InitialGenerationsCapacity];
            _dynamicAllocatedIds = new HashSet<int>();
            _current = _min - 1;
            _nextFreshIndex = _min;

            _ = MoveNextAndGetCurrent();
        }

        public int GetCurrent() => _current;

        private int GetNext()
        {
            int index;
            int generation;

            // Try to reuse from free queue first (FIFO - oldest released index)
            // FIFO ensures indices cycle through all free slots before reusing,
            // spreading generation increments evenly to prevent wrap-around in long matches
            if (_freeIndices.Count > 0)
            {
                index = _freeIndices.Dequeue();

                // Increment generation on reuse (with wraparound, skip 0)
                var currGen = _generations[index];
                if (currGen == ushort.MaxValue)
                    throw ElympicsLogger.LogException(new OverflowException($"Cannot use new networkId generation. The pool of generations (min: {0}, max: {ushort.MaxValue}) has been used up."));
                generation = ++_generations[index];
            }
            else
            {
                // Allocate new fresh index
                index = GetNextFreshIndex();
                EnsureGenerationCapacity(index);
                _generations[index] = 1; // First generation for dynamic objects
                generation = 1;
            }

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

            // Only recycle dynamic IDs (generation > 0) that match current generation
            // Scene objects (generation 0) are never recycled
            if (generation == 0)
                return;

            if (index >= _generations.Length || _generations[index] != generation)
            {
                ElympicsLogger.LogWarning($"Attempted to release stale or invalid NetworkId {networkId} (index={index}, gen={generation})");
                return;
            }

            // Add to back of free queue (FIFO - will be reused after all other free indices)
            _freeIndices.Enqueue(index);
        }

        public int MoveNextAndGetCurrent()
        {
            _current = GetNext();
            return _current;
        }

        public void MoveTo(int newCurrent) => _current = newCurrent;

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
