using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Elympics.Replication;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Elympics.Tests.Runtime.Replication
{
    [TestFixture]
    [Category("Replication")]
    public class ReplicationPipelineTests
    {
        private ReplicationPipeline _sut;

        [SetUp]
        public void SetUp()
        {
            ElympicsWorld.Current = null;
            // PipelineBuffers owned by ReplicationPipeline — no separate cleanup needed
            ReplicationPipeline.Current = null;
            _sut = null;
        }

        [TearDown]
        public void TearDown()
        {
            _sut?.Dispose();
            ElympicsWorld.Current?.Dispose();
            ElympicsWorld.Current = null;
            // PipelineBuffers owned by ReplicationPipeline — no separate cleanup needed
            ReplicationPipeline.Current = null;
        }

        #region Helper Methods

        /// <summary>
        /// Creates a pipeline with the given maxPlayers and activates playerCount players (cold start).
        /// </summary>
        private ReplicationPipeline CreatePipeline(int maxPlayers, int playerCount = -1)
        {
            if (playerCount < 0)
                playerCount = maxPlayers;

            // Create ElympicsWorld first and set the singleton
            var world = new ElympicsWorld(maxPlayers, 256, 512);
            ElympicsWorld.Current = world;

            // Create pipeline with the existing world
            var pipeline = new ReplicationPipeline(maxPlayers, world);

            // Activate players
            for (var i = 0; i < playerCount; i++)
                world.ActivatePlayer(i, ElympicsPlayer.FromIndex(i));

            return pipeline;
        }

        private static ElympicsSnapshot CreateSnapshot(long tick, params (int networkId, byte[] data)[] entities)
        {
            var data = new Dictionary<int, byte[]>();
            foreach (var (networkId, bytes) in entities)
                data[networkId] = bytes;

            var tickToInput = new Dictionary<int, TickToPlayerInput>();
            for (var i = 0; i < entities.Length; i++)
            {
                tickToInput[entities[i].networkId] = new TickToPlayerInput
                {
                    Data = new Dictionary<long, ElympicsSnapshotPlayerInput>()
                };
            }

            return new ElympicsSnapshot(
                tick,
                DateTime.UtcNow,
                new FactoryState(new Dictionary<int, FactoryPartState>()),
                data,
                tickToInput);
        }

        #endregion

        // =====================================================================
        // Category 1: Construction and Shutdown
        // =====================================================================

        [Test]
        public void Construct_SetsSingletons()
        {
            // Arrange & Act
            _sut = CreatePipeline(4);

            // Assert
            Assert.That(ElympicsWorld.Current, Is.Not.Null);
            Assert.That(ReplicationPipeline.Current.Buffers, Is.Not.Null);
            Assert.That(ReplicationPipeline.Current, Is.EqualTo(_sut));
        }

        [Test]
        public void Shutdown_ClearsPipelineSingletons()
        {
            // Arrange
            _sut = CreatePipeline(4);

            // Act
            _sut.Dispose();

            // Assert - pipeline shutdown only clears pipeline and buffers, NOT world
            Assert.That(ElympicsWorld.Current, Is.Not.Null); // World is still alive
            Assert.That(ReplicationPipeline.Current, Is.Null);
            Assert.That(ReplicationPipeline.Current, Is.Null);
        }

        // =====================================================================
        // Category 2: Full Tick Integration
        // =====================================================================

        [Test]
        public void Execute_TwoPlayersColdStart_AllEntitiesSentToBoth()
        {
            // Arrange
            _sut = CreatePipeline(2);
            var world = ElympicsWorld.Current;

            // Register entities visible to all
            world.RegisterEntity(10, ElympicsPlayer.All);
            world.RegisterEntity(20, ElympicsPlayer.All);

            var snapshot = CreateSnapshot(100, (10, new byte[] { 1 }), (20, new byte[] { 2 }));

            // Act
            world.BeginTick(snapshot, 100);
            _sut.Execute();

            // Assert
            var outputs = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs.Count, Is.EqualTo(2));

            var snap0 = outputs[ElympicsPlayer.FromIndex(0)];
            var snap1 = outputs[ElympicsPlayer.FromIndex(1)];

            Assert.That(snap0.Data.Count, Is.EqualTo(2));
            Assert.That(snap1.Data.Count, Is.EqualTo(2));
        }

        [Test]
        public void Execute_SecondTick_OnlyChangedEntitiesSent()
        {
            // Arrange
            _sut = CreatePipeline(2);
            var world = ElympicsWorld.Current;

            world.RegisterEntity(10, ElympicsPlayer.All);
            world.RegisterEntity(20, ElympicsPlayer.All);

            var snapshot1 = CreateSnapshot(100, (10, new byte[] { 1 }), (20, new byte[] { 2 }));

            // First tick
            world.BeginTick(snapshot1, 100);
            _sut.Execute();

            // Update player acknowledgements via SOA arrays
            world.PlayerLastReceivedSnapshot[0] = 100;
            world.PlayerLastReceivedSnapshot[1] = 100;

            // Second tick - only entity 10 changed
            var snapshot2 = CreateSnapshot(101, (10, new byte[] { 9 }), (20, new byte[] { 2 }));

            // Act
            world.BeginTick(snapshot2, 101);
            _sut.Execute();

            // Assert
            var outputs = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            var snap0 = outputs[ElympicsPlayer.FromIndex(0)];
            var snap1 = outputs[ElympicsPlayer.FromIndex(1)];

            Assert.That(snap0.Data.Count, Is.EqualTo(1)); // Only changed
            Assert.That(snap1.Data.Count, Is.EqualTo(1));
            Assert.That(snap0.Data.ContainsKey(10), Is.True);
        }

        [Test]
        public void Execute_InterestFiltering_DifferentSnapshotsPerPlayer()
        {
            // Arrange
            _sut = CreatePipeline(2);
            var world = ElympicsWorld.Current;

            world.RegisterEntity(10, ElympicsPlayer.FromIndex(0)); // Player 0 only
            world.RegisterEntity(20, ElympicsPlayer.FromIndex(1)); // Player 1 only

            var snapshot = CreateSnapshot(100, (10, new byte[] { 1 }), (20, new byte[] { 2 }));

            // Act
            world.BeginTick(snapshot, 100);
            _sut.Execute();

            // Assert
            var outputs = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            var snap0 = outputs[ElympicsPlayer.FromIndex(0)];
            var snap1 = outputs[ElympicsPlayer.FromIndex(1)];

            Assert.That(snap0.Data.Count, Is.EqualTo(1));
            Assert.That(snap0.Data.ContainsKey(10), Is.True);

            Assert.That(snap1.Data.Count, Is.EqualTo(1));
            Assert.That(snap1.Data.ContainsKey(20), Is.True);
        }

        [Test]
        public void Execute_EntityRegisteredBetweenTicks_AppearsInNextTick()
        {
            // Arrange
            _sut = CreatePipeline(2);
            var world = ElympicsWorld.Current;

            world.RegisterEntity(10, ElympicsPlayer.All);

            var snapshot1 = CreateSnapshot(100, (10, new byte[] { 1 }));

            // First tick
            world.BeginTick(snapshot1, 100);
            _sut.Execute();

            // Register new entity
            world.RegisterEntity(20, ElympicsPlayer.All);

            // Update acknowledgements
            world.PlayerLastReceivedSnapshot[0] = 100;
            world.PlayerLastReceivedSnapshot[1] = 100;

            // Second tick
            var snapshot2 = CreateSnapshot(101, (10, new byte[] { 1 }), (20, new byte[] { 2 }));

            // Act
            world.BeginTick(snapshot2, 101);
            _sut.Execute();

            // Assert
            var outputs = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            var snap0 = outputs[ElympicsPlayer.FromIndex(0)];

            Assert.That(snap0.Data.Count, Is.EqualTo(1)); // Only new entity 20
            Assert.That(snap0.Data.ContainsKey(20), Is.True);
        }

        [Test]
        public void Execute_EntityUnregisteredBetweenTicks_ExcludedFromOutput()
        {
            // Arrange
            _sut = CreatePipeline(2);
            var world = ElympicsWorld.Current;

            world.RegisterEntity(10, ElympicsPlayer.All);
            world.RegisterEntity(20, ElympicsPlayer.All);

            var snapshot1 = CreateSnapshot(100, (10, new byte[] { 1 }), (20, new byte[] { 2 }));

            // First tick
            world.BeginTick(snapshot1, 100);
            _sut.Execute();

            // Unregister entity 10
            world.UnregisterEntity(10);

            // Update acknowledgements
            world.PlayerLastReceivedSnapshot[0] = 100;
            world.PlayerLastReceivedSnapshot[1] = 100;

            // Second tick - entity 20 changed
            var snapshot2 = CreateSnapshot(101, (20, new byte[] { 9 }));

            // Act
            world.BeginTick(snapshot2, 101);
            _sut.Execute();

            // Assert
            var outputs = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            var snap0 = outputs[ElympicsPlayer.FromIndex(0)];

            Assert.That(snap0.Data.Count, Is.EqualTo(1)); // Only entity 20
            Assert.That(snap0.Data.ContainsKey(20), Is.True);
            Assert.That(snap0.Data.ContainsKey(10), Is.False);
        }

        [Test]
        public void Execute_BuffersClearedBetweenCalls()
        {
            // Arrange
            _sut = CreatePipeline(2);
            var world = ElympicsWorld.Current;

            world.RegisterEntity(10, ElympicsPlayer.All);

            var snapshot = CreateSnapshot(100, (10, new byte[] { 1 }));

            // First execute
            world.BeginTick(snapshot, 100);
            _sut.Execute();

            var buffersAfterFirst = ReplicationPipeline.Current.Buffers;
            Assert.That(buffersAfterFirst.OutputSnapshots.Count, Is.EqualTo(2));

            // Second execute
            world.PlayerLastReceivedSnapshot[0] = 100;
            world.PlayerLastReceivedSnapshot[1] = 100;
            world.BeginTick(snapshot, 100);
            _sut.Execute();

            // Assert - buffers cleared and repopulated; no entities changed so Data should be empty
            Assert.That(buffersAfterFirst.OutputSnapshots.Count, Is.EqualTo(2));
            Assert.That(buffersAfterFirst.OutputSnapshots[ElympicsPlayer.FromIndex(0)].Data.Count, Is.EqualTo(0));
            Assert.That(buffersAfterFirst.OutputSnapshots[ElympicsPlayer.FromIndex(1)].Data.Count, Is.EqualTo(0));
        }

        [Test]
        public void Execute_WorldEntity_ExcludedFromAllPlayers()
        {
            // Arrange
            _sut = CreatePipeline(2);
            var world = ElympicsWorld.Current;

            world.RegisterEntity(10, ElympicsPlayer.World); // World entity

            var snapshot = CreateSnapshot(100, (10, new byte[] { 1 }));

            // Act
            world.BeginTick(snapshot, 100);
            _sut.Execute();

            // Assert
            var outputs = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            var snap0 = outputs[ElympicsPlayer.FromIndex(0)];
            var snap1 = outputs[ElympicsPlayer.FromIndex(1)];

            Assert.That(snap0.Data.Count, Is.EqualTo(0)); // World entity excluded
            Assert.That(snap1.Data.Count, Is.EqualTo(0));
        }

        [Test]
        public void Execute_NoEntities_EmptyData_EmptyOutput()
        {
            // Arrange
            _sut = CreatePipeline(2);
            var world = ElympicsWorld.Current;

            var snapshot = CreateSnapshot(100); // No entities

            // Act
            world.BeginTick(snapshot, 100);
            _sut.Execute();

            // Assert
            var outputs = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            var snap0 = outputs[ElympicsPlayer.FromIndex(0)];
            var snap1 = outputs[ElympicsPlayer.FromIndex(1)];

            Assert.That(snap0.Data.Count, Is.EqualTo(0));
            Assert.That(snap1.Data.Count, Is.EqualTo(0));
        }

        [Test]
        public void Execute_NullSnapshotData_NoCrash_EmptyOutput()
        {
            // Arrange
            _sut = CreatePipeline(2);
            var world = ElympicsWorld.Current;

            var snapshot = new ElympicsSnapshot(
                100,
                DateTime.UtcNow,
                new FactoryState(new Dictionary<int, FactoryPartState>()),
                null, // Null data
                null);

            // Act & Assert - should not crash
            world.BeginTick(snapshot, 100);
            Assert.DoesNotThrow(() => _sut.Execute());

            var outputs = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs.Count, Is.EqualTo(0)); // No output
        }

        [Test]
        public void Execute_DoubleInit_LogsWarning()
        {
            // Arrange
            _sut = CreatePipeline(2);

            // Act & Assert - second init should warn
            LogAssert.Expect(LogType.Warning, new Regex(@"\[ReplicationPipeline\] Already initialized. Call Shutdown\(\) before re-initializing\."));
            var differentSut = new ReplicationPipeline(2, ElympicsWorld.Current);
            differentSut.Dispose();
        }

        [Test]
        public void Execute_ThreeTickScenario_ProgressiveAcknowledgement()
        {
            // Arrange
            _sut = CreatePipeline(2);
            var world = ElympicsWorld.Current;

            world.RegisterEntity(10, ElympicsPlayer.All);
            world.RegisterEntity(20, ElympicsPlayer.All);

            // Tick 100 - cold start, both entities sent
            var snapshot100 = CreateSnapshot(100, (10, new byte[] { 1 }), (20, new byte[] { 2 }));
            world.BeginTick(snapshot100, 100);
            _sut.Execute();
            var outputs100 = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs100[ElympicsPlayer.FromIndex(0)].Data.Count, Is.EqualTo(2));

            // Tick 101 - player 0 acks, entity 10 changes
            world.PlayerLastReceivedSnapshot[0] = 100;
            var snapshot101 = CreateSnapshot(101, (10, new byte[] { 9 }), (20, new byte[] { 2 }));
            world.BeginTick(snapshot101, 101);
            _sut.Execute();
            var outputs101 = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs101[ElympicsPlayer.FromIndex(0)].Data.Count, Is.EqualTo(1)); // Only 10
            Assert.That(outputs101[ElympicsPlayer.FromIndex(1)].Data.Count, Is.EqualTo(2)); // Still cold

            // Tick 102 - both players ack, entity 20 changes
            world.PlayerLastReceivedSnapshot[0] = 101;
            world.PlayerLastReceivedSnapshot[1] = 101;
            var snapshot102 = CreateSnapshot(102, (10, new byte[] { 9 }), (20, new byte[] { 8 }));
            world.BeginTick(snapshot102, 102);
            _sut.Execute();
            var outputs102 = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs102[ElympicsPlayer.FromIndex(0)].Data.Count, Is.EqualTo(1)); // Only 20
            Assert.That(outputs102[ElympicsPlayer.FromIndex(1)].Data.Count, Is.EqualTo(1)); // Only 20
            Assert.That(outputs102[ElympicsPlayer.FromIndex(0)].Data.ContainsKey(20), Is.True);
        }

        [Test]
        public void Execute_MixedVisibilityAndPartialDirtiness_EndToEnd()
        {
            // Arrange
            _sut = CreatePipeline(3);
            var world = ElympicsWorld.Current;

            world.RegisterEntity(10, ElympicsPlayer.All); // All players
            world.RegisterEntity(20, ElympicsPlayer.FromIndex(0)); // Player 0 only
            world.RegisterEntity(30, ElympicsPlayer.FromIndex(1)); // Player 1 only

            // Tick 100 - cold start
            var snapshot100 = CreateSnapshot(100,
                (10, new byte[] { 1 }),
                (20, new byte[] { 2 }),
                (30, new byte[] { 3 }));
            world.BeginTick(snapshot100, 100);
            _sut.Execute();

            var outputs100 = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs100[ElympicsPlayer.FromIndex(0)].Data.Count, Is.EqualTo(2)); // 10, 20
            Assert.That(outputs100[ElympicsPlayer.FromIndex(1)].Data.Count, Is.EqualTo(2)); // 10, 30
            Assert.That(outputs100[ElympicsPlayer.FromIndex(2)].Data.Count, Is.EqualTo(1)); // 10 only

            // Tick 101 - all ack, only entity 20 changes
            world.PlayerLastReceivedSnapshot[0] = 100;
            world.PlayerLastReceivedSnapshot[1] = 100;
            world.PlayerLastReceivedSnapshot[2] = 100;

            var snapshot101 = CreateSnapshot(101,
                (10, new byte[] { 1 }),
                (20, new byte[] { 9 }),
                (30, new byte[] { 3 }));
            world.BeginTick(snapshot101, 101);
            _sut.Execute();

            var outputs101 = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs101[ElympicsPlayer.FromIndex(0)].Data.Count, Is.EqualTo(1)); // Only 20
            Assert.That(outputs101[ElympicsPlayer.FromIndex(1)].Data.Count, Is.EqualTo(0)); // Nothing
            Assert.That(outputs101[ElympicsPlayer.FromIndex(2)].Data.Count, Is.EqualTo(0)); // Nothing
            Assert.That(outputs101[ElympicsPlayer.FromIndex(0)].Data.ContainsKey(20), Is.True);
        }

        // =====================================================================
        // Category 3: ActivePlayerCount < MaxPlayers
        // =====================================================================

        [Test]
        public void Execute_FewerActivePlayersThanMax_OnlyActivePlayersGetSnapshots()
        {
            // Arrange - maxPlayers=4 but only 2 active
            _sut = CreatePipeline(4, playerCount: 2);
            var world = ElympicsWorld.Current;

            world.RegisterEntity(10, ElympicsPlayer.All);

            var snapshot = CreateSnapshot(100, (10, new byte[] { 1 }));

            // Act
            world.BeginTick(snapshot, 100);
            _sut.Execute();

            // Assert - only 2 players get snapshots, not 4
            var outputs = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs.Count, Is.EqualTo(2));
            Assert.That(outputs.ContainsKey(ElympicsPlayer.FromIndex(0)), Is.True);
            Assert.That(outputs.ContainsKey(ElympicsPlayer.FromIndex(1)), Is.True);
            Assert.That(outputs.ContainsKey(ElympicsPlayer.FromIndex(2)), Is.False);
            Assert.That(outputs.ContainsKey(ElympicsPlayer.FromIndex(3)), Is.False);
        }

        [Test]
        public void Execute_ZeroActivePlayers_EmptyOutput()
        {
            // Arrange - maxPlayers=4 but 0 active (no players joined yet)
            _sut = CreatePipeline(4, playerCount: 0);
            var world = ElympicsWorld.Current;

            world.RegisterEntity(10, ElympicsPlayer.All);

            var snapshot = CreateSnapshot(100, (10, new byte[] { 1 }));

            // Act
            world.BeginTick(snapshot, 100);
            _sut.Execute();

            // Assert
            var outputs = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs.Count, Is.EqualTo(0));
        }

        [Test]
        public void Execute_NonContiguousActiveIndices_CorrectOutput()
        {
            // Arrange - maxPlayers=4, activate players 0 and 2 (skipping 1 and 3)
            var world = new ElympicsWorld(4, 256, 512);
            ElympicsWorld.Current = world;
            _sut = new ReplicationPipeline(4, world);

            world.ActivatePlayer(0, ElympicsPlayer.FromIndex(0));
            world.ActivatePlayer(2, ElympicsPlayer.FromIndex(2));

            world.RegisterEntity(10, ElympicsPlayer.All);

            var snapshot = CreateSnapshot(100, (10, new byte[] { 1 }));

            // Act
            world.BeginTick(snapshot, 100);
            _sut.Execute();

            // Assert - only players 0 and 2 get snapshots
            var outputs = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs.Count, Is.EqualTo(2));
            Assert.That(outputs.ContainsKey(ElympicsPlayer.FromIndex(0)), Is.True);
            Assert.That(outputs.ContainsKey(ElympicsPlayer.FromIndex(2)), Is.True);
            Assert.That(outputs.ContainsKey(ElympicsPlayer.FromIndex(1)), Is.False);
            Assert.That(outputs.ContainsKey(ElympicsPlayer.FromIndex(3)), Is.False);
        }

        // =====================================================================
        // Category 4: Ack-Aware Retransmission
        // =====================================================================

        [Test]
        public void Execute_AckAware_StampsLastSentTick_CorrectlyTracksAcks()
        {
            // Arrange
            _sut = CreatePipeline(2);
            var world = ElympicsWorld.Current;
            var buffers = ReplicationPipeline.Current.Buffers;

            world.RegisterEntity(10, ElympicsPlayer.All);
            world.RegisterEntity(20, ElympicsPlayer.All);

            var snapshot100 = CreateSnapshot(100, (10, new byte[] { 1 }), (20, new byte[] { 2 }));

            // Act - Tick 100: cold start, both entities sent to both players
            world.BeginTick(snapshot100, 100);
            _sut.Execute();

            // Assert tick 100: both players receive both entities
            var outputs100 = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs100[ElympicsPlayer.FromIndex(0)].Data.Count, Is.EqualTo(2));
            Assert.That(outputs100[ElympicsPlayer.FromIndex(1)].Data.Count, Is.EqualTo(2));

            // Assert LastSentTick stamped correctly for entity 10
            var dense10 = world.GetDenseIndex(10);
            var dense20 = world.GetDenseIndex(20);
            Assert.That(buffers.LastSentTick[0][dense10], Is.EqualTo(100));
            Assert.That(buffers.LastSentTick[1][dense10], Is.EqualTo(100));
            Assert.That(buffers.LastSentTick[0][dense20], Is.EqualTo(100));
            Assert.That(buffers.LastSentTick[1][dense20], Is.EqualTo(100));

            // Simulate ack: both players confirm receipt of tick 100
            world.PlayerLastReceivedSnapshot[0] = 100;
            world.PlayerLastReceivedSnapshot[1] = 100;

            // Act - Tick 101: same data (nothing changed), both players have acked tick 100
            // sentSinceChange: lastSentTick(100) >= lastModifiedTick(100) => true
            // clientAckedSend: lastRecv(100) >= lastSentTick(100) => true
            // => confirmed delivered, skip both entities for both players
            var snapshot101 = CreateSnapshot(101, (10, new byte[] { 1 }), (20, new byte[] { 2 }));
            world.BeginTick(snapshot101, 101);
            _sut.Execute();

            // Assert tick 101: both players receive 0 entities (confirmed delivered)
            var outputs101 = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs101[ElympicsPlayer.FromIndex(0)].Data.Count, Is.EqualTo(0));
            Assert.That(outputs101[ElympicsPlayer.FromIndex(1)].Data.Count, Is.EqualTo(0));
        }

        [Test]
        public void Execute_AckAware_UnackedEntity_ResentAfterNetUpdateInterval()
        {
            // Arrange - 1 player, entity 10 with High priority (interval=5), entity 20 with Normal (interval=15)
            _sut = CreatePipeline(1);
            var world = ElympicsWorld.Current;
            var buffers = ReplicationPipeline.Current.Buffers;

            world.RegisterEntity(10, ElympicsPlayer.All, 5); // interval = 5 ticks
            world.RegisterEntity(20, ElympicsPlayer.All, 15); // interval = 15 ticks

            var snapshotData = new byte[] { 1 };
            var snapshot100 = CreateSnapshot(100, (10, snapshotData), (20, snapshotData));

            // Act - Tick 100: cold start, both entities sent
            world.BeginTick(snapshot100, 100);
            _sut.Execute();

            // Assert tick 100: player receives both entities
            Assert.That(ReplicationPipeline.Current.Buffers.OutputSnapshots[ElympicsPlayer.FromIndex(0)].Data.Count, Is.EqualTo(2));

            var dense10 = world.GetDenseIndex(10);
            var dense20 = world.GetDenseIndex(20);
            Assert.That(buffers.LastSentTick[0][dense10], Is.EqualTo(100));

            // Simulate partial ack: player only confirmed tick 99, NOT tick 100
            // So lastRecv(99) < lastSentTick(100) => clientAckedSend = false
            // sentSinceChange: lastSentTick(100) >= lastModifiedTick(100) => true
            // => unacked re-send path, throttled by netUpdateInterval
            world.PlayerLastReceivedSnapshot[0] = 99;

            // Act - Tick 104: 4 ticks since send (100..104), interval=5 for entity 10, 15 for entity 20
            // currentTick(104) - lastSentTick(100) = 4 < 5 => entity 10 NOT re-sent
            // currentTick(104) - lastSentTick(100) = 4 < 15 => entity 20 NOT re-sent
            var snapshot104 = CreateSnapshot(104, (10, snapshotData), (20, snapshotData));
            world.BeginTick(snapshot104, 104);
            _sut.Execute();

            Assert.That(ReplicationPipeline.Current.Buffers.OutputSnapshots[ElympicsPlayer.FromIndex(0)].Data.Count, Is.EqualTo(0));

            // Act - Tick 105: 5 ticks since send (100..105), interval=5 for entity 10
            // currentTick(105) - lastSentTick(100) = 5 >= 5 => entity 10 IS re-sent
            // currentTick(105) - lastSentTick(100) = 5 < 15 => entity 20 NOT re-sent
            var snapshot105 = CreateSnapshot(105, (10, snapshotData), (20, snapshotData));
            world.BeginTick(snapshot105, 105);
            _sut.Execute();

            var outputs105 = ReplicationPipeline.Current.Buffers.OutputSnapshots;
            Assert.That(outputs105[ElympicsPlayer.FromIndex(0)].Data.Count, Is.EqualTo(1));
            Assert.That(outputs105[ElympicsPlayer.FromIndex(0)].Data.ContainsKey(10), Is.True);
            Assert.That(outputs105[ElympicsPlayer.FromIndex(0)].Data.ContainsKey(20), Is.False);

            // Assert LastSentTick for entity 10 updated to 105, entity 20 remains at 100
            Assert.That(buffers.LastSentTick[0][dense10], Is.EqualTo(105));
            Assert.That(buffers.LastSentTick[0][dense20], Is.EqualTo(100));
        }
    }
}
