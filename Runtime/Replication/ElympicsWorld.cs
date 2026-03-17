using System;
using Debug = UnityEngine.Debug;

namespace Elympics.Replication
{
    /// <summary>
    /// Observer for dense array layout changes in <see cref="ElympicsWorld"/>.
    /// Implemented by <see cref="PipelineBuffers"/> to keep its arrays in sync.
    /// </summary>
    internal interface IDenseLayoutObserver
    {
        void SwapRemoveDenseSlot(int denseIndex, int lastDenseIndex);
        void ResizeDenseDimension(int newDenseCapacity);
    }

    /// <summary>
    /// Persistent per-entity replication state that lives across ticks.
    /// Maintains a sparse-to-dense mapping so that hot-path iteration operates
    /// on packed, contiguous arrays with no gaps.
    /// Also owns player arrays for thread-safe, consistent pipeline reads.
    /// </summary>
    internal sealed class ElympicsWorld : IDisposable
    {
        /// <summary>
        /// Initial capacity for dense arrays. Arrays grow dynamically as needed.
        /// </summary>
        private const int InitialDenseCapacity = 128;

        internal static ElympicsWorld Current { get; set; }

        internal ElympicsSnapshot CurrentSnapshot { get; private set; }
        internal ElympicsSnapshot PreviousSnapshot { get; private set; }
        internal int MaxPlayers { get; private set; }
        internal long CurrentTick { get; private set; }

        // ── Player arrays (sized by MaxPlayers, indexed by player index) ──
        internal ElympicsPlayer[] PlayerIds { get; private set; }
        internal long[] PlayerLastReceivedSnapshot { get; private set; }

        /// <summary>
        /// Backing array for active player indices. Only entries <c>[0, ActivePlayersCount)</c> are valid.
        /// Pipeline systems iterate this via a <see cref="PackedArray{T}"/> view instead of checking a bool[] per player slot.
        /// </summary>
        internal int[] ActivePlayers { get; private set; }

        /// <summary>Number of valid entries in <see cref="ActivePlayers"/>.</summary>
        internal int ActivePlayersCount { get; private set; }

        /// <summary>
        /// Thread-safe queue for player state updates from the network thread.
        /// Drained at tick start before pipeline execution.
        /// </summary>
        internal PlayerStateUpdateQueue PlayerUpdateQueue { get; private set; }

        internal int[] SparseToDense { get; private set; }
        private int[] _currentGeneration;

        internal int[] DenseToSparse { get; private set; }
        internal int DenseCount { get; private set; }
        internal int DenseCapacity { get; private set; }
        private int _maxDenseCapacity;
        private readonly int _maxSparseSlots;
        internal long[] LastModifiedTick { get; private set; }
        internal uint[] InterestMask { get; private set; }
        internal int[] NetUpdateInterval { get; private set; }

        /// <summary>
        /// Optional observer notified when the dense array layout changes (grow, shrink, swap-remove).
        /// Set by <see cref="ReplicationPipeline"/> on construction; null on client where no pipeline exists.
        /// </summary>
        internal IDenseLayoutObserver DenseLayoutObserver { get; set; }

        internal ElympicsWorld(int maxPlayers, int maxSparseSlots = NetworkIdConstants.MaxIndex + 1, int maxDenseEntities = NetworkIdConstants.MaxNetworkObjects)
        {
            Debug.Assert(maxPlayers <= 32, $"[ElympicsWorld] maxPlayers ({maxPlayers}) exceeds 32. Interest bitmask uses uint (32 bits).");
            MaxPlayers = maxPlayers;
            _maxDenseCapacity = maxDenseEntities;
            _maxSparseSlots = maxSparseSlots;

            PlayerIds = new ElympicsPlayer[maxPlayers];
            PlayerLastReceivedSnapshot = new long[maxPlayers];
            ActivePlayers = new int[maxPlayers];
            ActivePlayersCount = 0;

            PlayerUpdateQueue = new PlayerStateUpdateQueue();

            SparseToDense = new int[maxSparseSlots];
            _currentGeneration = new int[maxSparseSlots];

            DenseCapacity = Math.Min(InitialDenseCapacity, _maxDenseCapacity);
            DenseToSparse = new int[DenseCapacity];
            LastModifiedTick = new long[DenseCapacity];
            InterestMask = new uint[DenseCapacity];
            NetUpdateInterval = new int[DenseCapacity];
            CurrentSnapshot = ElympicsSnapshot.CreateEmpty();
            PreviousSnapshot = ElympicsSnapshot.CreateEmpty();
            CurrentTick = -1;

            // Fill sentinel values for freshly allocated arrays.
            FillSentinels();
        }

        /// <summary>
        /// Resets all world state. Clears entity registrations, player activation,
        /// snapshots, tick counter, and discards stale queued updates.
        /// </summary>
        internal void Reset()
        {
            FillSentinels();

            CurrentSnapshot = ElympicsSnapshot.CreateEmpty();
            PreviousSnapshot = ElympicsSnapshot.CreateEmpty();
            CurrentTick = -1;

            PlayerUpdateQueue.Reset();
        }

        private void FillSentinels()
        {
            ActivePlayersCount = 0;

            for (var p = 0; p < MaxPlayers; p++)
            {
                PlayerIds[p] = ElympicsPlayer.Invalid;
                PlayerLastReceivedSnapshot[p] = -1;
            }

            for (var i = 0; i < _maxSparseSlots; i++)
            {
                SparseToDense[i] = -1;
                _currentGeneration[i] = -1;
            }

            DenseCount = 0;
        }

        /// <summary>
        /// Marks a player slot as active and assigns the player identity.
        /// Appends <paramref name="playerIndex"/> to <see cref="ActivePlayers"/>.
        /// Called by GameEngineAdapter.Initialize() after the match is set up.
        /// </summary>
        internal void ActivatePlayer(int playerIndex, ElympicsPlayer id)
        {
            if (playerIndex < 0 || playerIndex >= MaxPlayers)
            {
                ElympicsLogger.LogError($"[ElympicsWorld] Cannot activate player at index {playerIndex}: out of range [0, {MaxPlayers}).");
                return;
            }

            // Check for duplicate activation by scanning the active players array.
            for (var i = 0; i < ActivePlayersCount; i++)
            {
                if (ActivePlayers[i] == playerIndex)
                {
                    ElympicsLogger.LogWarning($"[ElympicsWorld] Player at index {playerIndex} already active. Skipping.");
                    return;
                }
            }

            PlayerIds[playerIndex] = id;
            PlayerLastReceivedSnapshot[playerIndex] = -1;

            ActivePlayers[ActivePlayersCount] = playerIndex;
            ActivePlayersCount++;
        }

        public void Dispose()
        {
            SparseToDense = null;
            _currentGeneration = null;
            DenseToSparse = null;
            LastModifiedTick = null;
            InterestMask = null;
            NetUpdateInterval = null;

            CurrentSnapshot = null;
            PreviousSnapshot = null;

            PlayerIds = null;
            PlayerLastReceivedSnapshot = null;
            ActivePlayers = null;
            ActivePlayersCount = 0;
            PlayerUpdateQueue = null;
            DenseLayoutObserver = null;

            DenseCount = 0;
            DenseCapacity = 0;
            _maxDenseCapacity = 0;
            MaxPlayers = 0;
            CurrentTick = -1;

            Current = null;
        }

        internal void BeginTick(ElympicsSnapshot fullSnapshot, long tick)
        {
            // Drain queued player state updates from the network thread
            // before any pipeline reads, ensuring consistent data for this tick.
            PlayerUpdateQueue.DrainTo(PlayerLastReceivedSnapshot);

            PreviousSnapshot = CurrentSnapshot;
            CurrentSnapshot = fullSnapshot;
            CurrentTick = tick;
        }

        /// <summary>
        /// Doubles the capacity of all dense arrays when DenseCount exceeds current capacity.
        /// Also notifies PipelineBuffers to resize its dense dimension.
        /// </summary>
        private void GrowDenseArrays()
        {
            var newCapacity = Math.Min(DenseCapacity * 2, _maxDenseCapacity);
            if (newCapacity <= DenseCapacity)
                return; // at max, can't grow
            DenseToSparse = DenseToSparse.Resized(newCapacity, DenseCount);
            LastModifiedTick = LastModifiedTick.Resized(newCapacity, DenseCount);
            InterestMask = InterestMask.Resized(newCapacity, DenseCount);
            NetUpdateInterval = NetUpdateInterval.Resized(newCapacity, DenseCount);
            DenseCapacity = newCapacity;

            DenseLayoutObserver?.ResizeDenseDimension(newCapacity);
        }

        /// <summary>
        /// Halves the capacity of all dense arrays when DenseCount drops to 3/8 of capacity
        /// and the new capacity would still be >= InitialDenseCapacity.
        /// Also notifies PipelineBuffers to resize its dense dimension.
        /// </summary>
        private void ShrinkDenseArrays()
        {
            var newCapacity = DenseCapacity / 2;
            DenseToSparse = DenseToSparse.Resized(newCapacity, DenseCount);
            LastModifiedTick = LastModifiedTick.Resized(newCapacity, DenseCount);
            InterestMask = InterestMask.Resized(newCapacity, DenseCount);
            NetUpdateInterval = NetUpdateInterval.Resized(newCapacity, DenseCount);
            DenseCapacity = newCapacity;

            DenseLayoutObserver?.ResizeDenseDimension(newCapacity);
        }

        internal void RegisterEntity(int networkId, ElympicsPlayer visibleFor, int netUpdateInterval = -1)
        {
            var sparseIndex = ExtractIndex(networkId);
            var generation = ExtractGeneration(networkId);
            if (sparseIndex >= SparseToDense.Length)
            {
                ElympicsLogger.LogError($"[ElympicsWorld] Entity {networkId} sparse index {sparseIndex} exceeds capacity {SparseToDense.Length}.");
                return;
            }

            if (SparseToDense[sparseIndex] >= 0)
            {
                ElympicsLogger.LogWarning($"[ElympicsWorld] Entity {networkId} already registered at dense index {SparseToDense[sparseIndex]}. Skipping.");
                return;
            }

            if (DenseCount >= _maxDenseCapacity)
            {
                ElympicsLogger.LogError($"[ElympicsWorld] Cannot register entity {networkId}: at maximum capacity ({_maxDenseCapacity}).");
                return;
            }

            if (DenseCount >= DenseCapacity)
                GrowDenseArrays();

            var denseIndex = DenseCount;
            DenseCount++;

            // Update mappings
            SparseToDense[sparseIndex] = denseIndex;
            DenseToSparse[denseIndex] = networkId;
            _currentGeneration[sparseIndex] = generation;

            // Initialize dense data
            LastModifiedTick[denseIndex] = 0;
            InterestMask[denseIndex] = InterestManagementSystem.ConvertVisibleFor(visibleFor, MaxPlayers);
            NetUpdateInterval[denseIndex] = netUpdateInterval > 0 ? netUpdateInterval : 1;
        }

        internal void UnregisterEntity(int networkId)
        {
            var sparseIndex = ExtractIndex(networkId);
            if (sparseIndex >= SparseToDense.Length)
            {
                ElympicsLogger.LogError($"[ElympicsWorld] Entity {networkId} sparse index {sparseIndex} exceeds capacity {SparseToDense.Length}.");
                return;
            }

            var denseIndex = SparseToDense[sparseIndex];

            if (denseIndex < 0)
            {
                ElympicsLogger.LogWarning($"[ElympicsWorld] Entity {networkId} not registered. Skipping unregister.");
                return;
            }

            var lastDenseIndex = DenseCount - 1;

            // Swap-remove: move last entity into the hole (if not already the last)
            if (denseIndex != lastDenseIndex)
            {
                var lastNetworkId = DenseToSparse[lastDenseIndex];
                var lastSparseIndex = ExtractIndex(lastNetworkId);

                // Update mappings
                DenseToSparse[denseIndex] = lastNetworkId;
                SparseToDense[lastSparseIndex] = denseIndex;

                // Copy all parallel dense array data from [last] to [denseIndex]
                LastModifiedTick[denseIndex] = LastModifiedTick[lastDenseIndex];
                InterestMask[denseIndex] = InterestMask[lastDenseIndex];
                NetUpdateInterval[denseIndex] = NetUpdateInterval[lastDenseIndex];
            }

            DenseLayoutObserver?.SwapRemoveDenseSlot(denseIndex, lastDenseIndex);

            // Invalidate removed entity
            SparseToDense[sparseIndex] = -1;
            _currentGeneration[sparseIndex] = -1;
            DenseCount--;

            // Shrink arrays if count drops to 3/8 of capacity and halving wouldn't go below minimum
            var shrinkThreshold = DenseCapacity * 3 / 8;
            var newCapacity = DenseCapacity / 2;
            if (DenseCount <= shrinkThreshold && newCapacity >= InitialDenseCapacity)
                ShrinkDenseArrays();
        }

        internal static int ExtractIndex(int networkId) => networkId & NetworkIdConstants.IndexMask;

        internal static int ExtractGeneration(int networkId) => (networkId >> NetworkIdConstants.GenerationShift) & NetworkIdConstants.IndexMask;

        internal int GetDenseIndex(int networkId)
        {
            var sparseIndex = ExtractIndex(networkId);
            return SparseToDense[sparseIndex];
        }

        internal int GetNetworkId(int denseIndex) => DenseToSparse[denseIndex];

        internal bool IsValid(int networkId)
        {
            var sparseIndex = ExtractIndex(networkId);
            var generation = ExtractGeneration(networkId);
            return _currentGeneration[sparseIndex] == generation;
        }
    }
}
