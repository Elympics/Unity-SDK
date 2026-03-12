using System;
using System.Collections.Generic;

namespace Elympics.Replication
{
    /// <summary>
    /// Pre-allocated scratch buffers used by the replication pipeline each tick.
    /// All 2D buffers are indexed [playerIndex][entitySlot] and store dense entity indices.
    /// Row counts are reset every tick; the underlying arrays are never reallocated during normal operation.
    /// Pipeline systems receive <see cref="PackedArray2D{T}"/> views over these raw arrays.
    /// </summary>
    internal sealed class PipelineBuffers : IDenseLayoutObserver, IDisposable
    {
        /// <summary>
        /// Dense indices of entities visible to each player after interest filtering.
        /// Written by <see cref="InterestManagementSystem"/>, read by <see cref="PrioritizationSystem"/>.
        /// </summary>
        internal int[][] RelevantEntities { get; private set; }
        /// <summary>
        /// Number of valid entries in each row of <see cref="RelevantEntities"/>.
        /// </summary>
        internal int[] RelevantCounts { get; private set; }

        /// <summary>
        /// Subset of <see cref="RelevantEntities"/> that actually changed since the player last received them.
        /// Written by <see cref="PrioritizationSystem"/>, read by <see cref="BandwidthSchedulingSystem"/>.
        /// </summary>
        internal int[][] DirtySorted { get; private set; }
        /// <summary>
        /// Number of valid entries in each row of <see cref="DirtySorted"/>.
        /// </summary>
        internal int[] DirtySortedCounts { get; private set; }

        /// <summary>
        /// Final set of dense indices per player selected for serialization after bandwidth limits are applied.
        /// Written by <see cref="BandwidthSchedulingSystem"/>, read by <see cref="SnapshotEncoderSystem"/>.
        /// </summary>
        internal int[][] Scheduled { get; private set; }
        /// <summary>
        /// Number of valid entries in each row of <see cref="Scheduled"/>.
        /// </summary>
        internal int[] ScheduledCounts { get; private set; }

        /// <summary>
        /// Per-player snapshots produced by <see cref="SnapshotEncoderSystem"/> at the end of the pipeline.
        /// Keyed by <see cref="ElympicsPlayer"/> so the server can send each player only their relevant state.
        /// </summary>
        internal Dictionary<ElympicsPlayer, ElympicsSnapshot> OutputSnapshots { get; private set; }

        /// <summary>
        /// Persistent per-player, per-entity tick when the entity was last included in a snapshot
        /// sent to that player. Indexed [playerIndex][denseIndex]. Default 0 is safe: cold-start
        /// players have lastRecv = -1 which bypasses all ack checks.
        /// NOT cleared each tick — persists across ticks.
        /// </summary>
        internal long[][] LastSentTick { get; private set; }

        private readonly int _maxPlayers;
        private int _denseCapacity;

        internal PipelineBuffers(int maxPlayers, int denseCapacity)
        {
            _maxPlayers = maxPlayers;
            _denseCapacity = denseCapacity;

            RelevantEntities = CreateJagged<int>(maxPlayers, denseCapacity);
            RelevantCounts = new int[maxPlayers];

            DirtySorted = CreateJagged<int>(maxPlayers, denseCapacity);
            DirtySortedCounts = new int[maxPlayers];

            Scheduled = CreateJagged<int>(maxPlayers, denseCapacity);
            ScheduledCounts = new int[maxPlayers];

            OutputSnapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>(maxPlayers);

            LastSentTick = CreateJagged<long>(maxPlayers, denseCapacity);
        }

        /// <summary>
        /// Resizes the dense dimension of all 2D buffers to match a new capacity.
        /// Called via <see cref="IDenseLayoutObserver"/> when ElympicsWorld dense arrays grow or shrink.
        /// Scratch buffers are reallocated without copying. LastSentTick is preserved.
        /// </summary>
        public void ResizeDenseDimension(int newDenseCapacity)
        {
            _denseCapacity = newDenseCapacity;

            RelevantEntities = CreateJagged<int>(_maxPlayers, _denseCapacity);
            RelevantCounts = new int[_maxPlayers];

            DirtySorted = CreateJagged<int>(_maxPlayers, _denseCapacity);
            DirtySortedCounts = new int[_maxPlayers];

            Scheduled = CreateJagged<int>(_maxPlayers, _denseCapacity);
            ScheduledCounts = new int[_maxPlayers];

            // Preserve LastSentTick data across resize
            var oldLastSentTick = LastSentTick;
            LastSentTick = CreateJagged<long>(_maxPlayers, _denseCapacity);
            var copyCount = Math.Min(oldLastSentTick[0].Length, _denseCapacity);
            for (var p = 0; p < _maxPlayers; p++)
                Array.Copy(oldLastSentTick[p], LastSentTick[p], copyCount);
        }

        internal void Clear()
        {
            Array.Clear(RelevantCounts, 0, _maxPlayers);
            Array.Clear(DirtySortedCounts, 0, _maxPlayers);
            Array.Clear(ScheduledCounts, 0, _maxPlayers);
            OutputSnapshots.Clear();
            // Note: LastSentTick is intentionally NOT cleared — it persists across ticks.
        }

        /// <summary>
        /// Mirrors ElympicsWorld swap-remove: when a dense slot is removed, move the last
        /// slot's LastSentTick data into the hole and zero the vacated last slot.
        /// Called via <see cref="IDenseLayoutObserver"/>.
        /// </summary>
        public void SwapRemoveDenseSlot(int denseIndex, int lastDenseIndex)
        {
            if (denseIndex != lastDenseIndex)
            {
                for (var p = 0; p < _maxPlayers; p++)
                {
                    LastSentTick[p][denseIndex] = LastSentTick[p][lastDenseIndex];
                    LastSentTick[p][lastDenseIndex] = 0;
                }
            }
            else
            {
                for (var p = 0; p < _maxPlayers; p++)
                    LastSentTick[p][denseIndex] = 0;
            }
        }

        public void Dispose()
        {
            RelevantEntities = null;
            RelevantCounts = null;
            DirtySorted = null;
            DirtySortedCounts = null;
            Scheduled = null;
            ScheduledCounts = null;

            OutputSnapshots = null;
            LastSentTick = null;
        }

        private static T[][] CreateJagged<T>(int rows, int columns)
        {
            var array = new T[rows][];
            for (var i = 0; i < rows; i++)
                array[i] = new T[columns];
            return array;
        }
    }
}
