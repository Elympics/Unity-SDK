using System;
using Elympics.Core.Logger;

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
            var lastModifiedTick = _world.LastModifiedTick;

            ChangeDetectionSystem.Execute(
                currentData,
                previousData,
                currentTick,
                ref lastModifiedTick,
                _world.SparseToDense);

            var activePlayers = new PackedArray<int>(_world.ActivePlayers, _world.ActivePlayersCount);
            var relevantEntities = new PackedArray2D<int>(Buffers.RelevantEntities, Buffers.RelevantCounts);

            InterestManagementSystem.Execute(
                currentData,
                _world.InterestMask,
                activePlayers,
                _world.SparseToDense,
                ref relevantEntities);

            var dirtySorted = new PackedArray2D<int>(Buffers.DirtySorted, Buffers.DirtySortedCounts);

            PrioritizationSystem.Execute(
                _world.PlayerLastReceivedSnapshot,
                activePlayers,
                _world.LastModifiedTick,
                Buffers.LastSentTick,
                currentTick,
                _world.NetUpdateInterval,
                relevantEntities,
                ref dirtySorted);

            var scheduled = new PackedArray2D<int>(Buffers.Scheduled, Buffers.ScheduledCounts);

            BandwidthSchedulingSystem.Execute(
                activePlayers,
                dirtySorted,
                ref scheduled);

            var outputSnapshots = Buffers.OutputSnapshots;

            SnapshotEncoderSystem.Execute(
                _world.CurrentSnapshot,
                _world.PlayerIds,
                activePlayers,
                scheduled,
                _world.DenseToSparse,
                ref outputSnapshots);

            var lastSentTick = Buffers.LastSentTick;

            AckTrackingSystem.Execute(
                activePlayers,
                scheduled,
                ref lastSentTick,
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
