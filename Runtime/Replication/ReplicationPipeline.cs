using System;

namespace Elympics.Replication
{
    /// <summary>
    /// Orchestrates the server-side replication pipeline. Each tick it runs a sequence of
    /// systems that detect changed NetworkEntities, filter by player visibility, apply dirty/priority
    /// filtering, enforce bandwidth limits, and produce per-player snapshots ready for sending.
    /// </summary>
    internal sealed class ReplicationPipeline : IDisposable
    {
        internal static ReplicationPipeline Current { get; set; }

        private readonly ElympicsWorld _world;

        internal PipelineBuffers Buffers { get; }

        public ReplicationPipeline(int maxPlayers, ElympicsWorld world)
        {
            if (Current != null)
                ElympicsLogger.LogWarning("[ReplicationPipeline] Already initialized. Call Shutdown() before re-initializing.");

            _world = world;
            Buffers = new PipelineBuffers(maxPlayers, _world.DenseCapacity);
            _world.DenseLayoutObserver = Buffers;

            Current = this;
        }

        public static void Initialize(int maxPlayers, ElympicsWorld world) => _ = new ReplicationPipeline(maxPlayers, world);

        internal void Execute()
        {
            Buffers.Clear();

            var currentData = _world.CurrentSnapshot.Data;
            var previousData = _world.PreviousSnapshot.Data;
            var currentTick = _world.CurrentTick;

            ChangeDetectionSystem.Execute(
                currentData,
                previousData,
                currentTick,
                _world.LastModifiedTick,
                _world.SparseToDense);

            var activePlayers = new PackedArray<int>(_world.ActivePlayers, _world.ActivePlayersCount);
            var relevantEntities = new PackedArray2D<int>(Buffers.RelevantEntities, Buffers.RelevantCounts);
            var dirtySorted = new PackedArray2D<int>(Buffers.DirtySorted, Buffers.DirtySortedCounts);
            var scheduled = new PackedArray2D<int>(Buffers.Scheduled, Buffers.ScheduledCounts);

            InterestManagementSystem.Execute(
                currentData,
                _world.InterestMask,
                activePlayers,
                _world.SparseToDense,
                relevantEntities);

            PrioritizationSystem.Execute(
                _world.PlayerLastReceivedSnapshot,
                activePlayers,
                _world.LastModifiedTick,
                Buffers.LastSentTick,
                currentTick,
                _world.NetUpdateInterval,
                relevantEntities,
                dirtySorted);

            BandwidthSchedulingSystem.Execute(
                activePlayers,
                dirtySorted,
                scheduled);

            SnapshotEncoderSystem.Execute(
                _world.CurrentSnapshot,
                _world.PlayerIds,
                activePlayers,
                scheduled,
                _world.DenseToSparse,
                Buffers.OutputSnapshots);

            AckTrackingSystem.Execute(
                activePlayers,
                scheduled,
                Buffers.LastSentTick,
                currentTick);
        }

        public void Dispose()
        {
            Current = null;
            _world.DenseLayoutObserver = null;
            Buffers.Dispose();
        }
    }
}
