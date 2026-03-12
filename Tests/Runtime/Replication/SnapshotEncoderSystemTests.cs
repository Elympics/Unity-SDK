using System;
using System.Collections.Generic;
using Elympics.Replication;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Replication
{
    [TestFixture]
    [Category("Replication")]
    public class SnapshotEncoderSystemTests
    {
        #region Helper Methods

        private static int[][] CreateJagged(int rows, int cols)
        {
            var arr = new int[rows][];
            for (var i = 0; i < rows; i++)
                arr[i] = new int[cols];
            return arr;
        }

        /// <summary>
        /// Creates playerIds and an active player index array [0..count-1].
        /// </summary>
        private static ElympicsPlayer[] CreatePlayerArrays(int count, out int[] activeArray)
        {
            var ids = new ElympicsPlayer[count];
            activeArray = new int[count];
            for (var i = 0; i < count; i++)
            {
                ids[i] = ElympicsPlayer.FromIndex(i);
                activeArray[i] = i;
            }
            return ids;
        }

        private static ElympicsSnapshot CreateSnapshot(long tick, params (int networkId, byte[] data)[] entities)
        {
            var data = new Dictionary<int, byte[]>();
            foreach (var (networkId, bytes) in entities)
                data[networkId] = bytes;

            var tickToInput = new Dictionary<int, TickToPlayerInput>();
            for (var i = 0; i < entities.Length; i++)
            {
                tickToInput[i] = new TickToPlayerInput
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
        // Category 1: Basic Encoding
        // =====================================================================

        [Test]
        public void Execute_PerPlayerSnapshots_OnlyScheduledEntities()
        {
            // Arrange
            var playerIds = CreatePlayerArrays(2, out var activeArray);
            var activePlayers = new PackedArray<int>(activeArray, activeArray.Length);

            var fullSnapshot = CreateSnapshot(100,
                (10, new byte[] { 1 }),
                (20, new byte[] { 2 }),
                (30, new byte[] { 3 }));

            var scheduled = new PackedArray2D<int>(CreateJagged(2, 128), new int[2]);
            scheduled.Append(0, 0); // Dense index 0 -> networkId 10
            scheduled.Append(0, 1); // Dense index 1 -> networkId 20
            scheduled.Append(1, 2); // Dense index 2 -> networkId 30

            var denseToSparse = new int[128];
            denseToSparse[0] = 10;
            denseToSparse[1] = 20;
            denseToSparse[2] = 30;

            var outputSnapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>();

            // Act
            SnapshotEncoderSystem.Execute(fullSnapshot, playerIds, activePlayers, scheduled, denseToSparse, outputSnapshots);

            // Assert
            Assert.That(outputSnapshots.Count, Is.EqualTo(2));

            var snap0 = outputSnapshots[ElympicsPlayer.FromIndex(0)];
            Assert.That(snap0.Data.Count, Is.EqualTo(2));
            Assert.That(snap0.Data.ContainsKey(10), Is.True);
            Assert.That(snap0.Data.ContainsKey(20), Is.True);

            var snap1 = outputSnapshots[ElympicsPlayer.FromIndex(1)];
            Assert.That(snap1.Data.Count, Is.EqualTo(1));
            Assert.That(snap1.Data.ContainsKey(30), Is.True);
        }

        [Test]
        public void Execute_OwnPlayerInput_Removed()
        {
            // Arrange
            var playerIds = CreatePlayerArrays(2, out var activeArray);
            var activePlayers = new PackedArray<int>(activeArray, activeArray.Length);

            var fullSnapshot = CreateSnapshot(100,
                (10, new byte[] { 1 }),
                (20, new byte[] { 2 }));

            var scheduled = new PackedArray2D<int>(CreateJagged(2, 128), new int[2]);
            scheduled.Append(0, 0); // networkId 10
            scheduled.Append(0, 1); // networkId 20
            scheduled.Append(1, 0);
            scheduled.Append(1, 1);

            var denseToSparse = new int[128];
            denseToSparse[0] = 10;
            denseToSparse[1] = 20;

            var outputSnapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>();

            // Act
            SnapshotEncoderSystem.Execute(fullSnapshot, playerIds, activePlayers, scheduled, denseToSparse, outputSnapshots);

            // Assert
            var snap0 = outputSnapshots[ElympicsPlayer.FromIndex(0)];
            var snap1 = outputSnapshots[ElympicsPlayer.FromIndex(1)];

            // Each player should not have their own input (key is player index)
            Assert.That(snap0.TickToPlayersInputData.ContainsKey(0), Is.False);
            Assert.That(snap0.TickToPlayersInputData.ContainsKey(1), Is.True);

            Assert.That(snap1.TickToPlayersInputData.ContainsKey(0), Is.True);
            Assert.That(snap1.TickToPlayersInputData.ContainsKey(1), Is.False);
        }

        [Test]
        public void Execute_NullFullSnapshot_NoOutput()
        {
            // Arrange
            var playerIds = CreatePlayerArrays(1, out var activeArray);
            var activePlayers = new PackedArray<int>(activeArray, activeArray.Length);
            ElympicsSnapshot fullSnapshot = null;

            var scheduled = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]); // empty row
            var denseToSparse = new int[128];
            var outputSnapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>();

            // Act & Assert - should not throw
            SnapshotEncoderSystem.Execute(fullSnapshot, playerIds, activePlayers, scheduled, denseToSparse, outputSnapshots);

            Assert.That(outputSnapshots.Count, Is.EqualTo(0));
        }

        [Test]
        public void Execute_NullSnapshotData_NoOutput()
        {
            // Arrange
            var playerIds = CreatePlayerArrays(1, out var activeArray);
            var activePlayers = new PackedArray<int>(activeArray, activeArray.Length);

            var fullSnapshot = new ElympicsSnapshot(
                100,
                DateTime.UtcNow,
                new FactoryState(new Dictionary<int, FactoryPartState>()),
                null, // Null data
                null);

            var scheduled = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            scheduled.Append(0, 0);

            var denseToSparse = new int[128];
            var outputSnapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>();

            // Act
            SnapshotEncoderSystem.Execute(fullSnapshot, playerIds, activePlayers, scheduled, denseToSparse, outputSnapshots);

            // Assert
            Assert.That(outputSnapshots.Count, Is.EqualTo(0));
        }

        [Test]
        public void Execute_NullTickToPlayersInputData_NullInOutput()
        {
            // Arrange
            var playerIds = CreatePlayerArrays(1, out var activeArray);
            var activePlayers = new PackedArray<int>(activeArray, activeArray.Length);

            var data = new Dictionary<int, byte[]> { [10] = new byte[] { 1 } };
            var fullSnapshot = new ElympicsSnapshot(
                100,
                DateTime.UtcNow,
                new FactoryState(new Dictionary<int, FactoryPartState>()),
                data,
                null); // Null input data

            var scheduled = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            scheduled.Append(0, 0);

            var denseToSparse = new int[128];
            denseToSparse[0] = 10;

            var outputSnapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>();

            // Act
            SnapshotEncoderSystem.Execute(fullSnapshot, playerIds, activePlayers, scheduled, denseToSparse, outputSnapshots);

            // Assert
            var snap = outputSnapshots[ElympicsPlayer.FromIndex(0)];
            Assert.That(snap.TickToPlayersInputData, Is.Null);
        }

        [Test]
        public void Execute_ZeroScheduled_EmptyDataDict()
        {
            // Arrange
            var playerIds = CreatePlayerArrays(1, out var activeArray);
            var activePlayers = new PackedArray<int>(activeArray, activeArray.Length);

            var fullSnapshot = CreateSnapshot(100, (10, new byte[] { 1 }));

            var scheduled = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]); // empty row (count = 0)
            var denseToSparse = new int[128];
            var outputSnapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>();

            // Act
            SnapshotEncoderSystem.Execute(fullSnapshot, playerIds, activePlayers, scheduled, denseToSparse, outputSnapshots);

            // Assert
            var snap = outputSnapshots[ElympicsPlayer.FromIndex(0)];
            Assert.That(snap.Data.Count, Is.EqualTo(0));
        }

        [Test]
        public void Execute_EntityNotInFullSnapshot_Skipped()
        {
            // Arrange
            var playerIds = CreatePlayerArrays(1, out var activeArray);
            var activePlayers = new PackedArray<int>(activeArray, activeArray.Length);

            var fullSnapshot = CreateSnapshot(100, (10, new byte[] { 1 })); // Only has networkId 10

            var scheduled = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            scheduled.Append(0, 0); // networkId 10
            scheduled.Append(0, 1); // networkId 20 (not in fullSnapshot)

            var denseToSparse = new int[128];
            denseToSparse[0] = 10;
            denseToSparse[1] = 20;

            var outputSnapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>();

            // Act
            SnapshotEncoderSystem.Execute(fullSnapshot, playerIds, activePlayers, scheduled, denseToSparse, outputSnapshots);

            // Assert
            var snap = outputSnapshots[ElympicsPlayer.FromIndex(0)];
            Assert.That(snap.Data.Count, Is.EqualTo(1)); // Only 10, 20 skipped
            Assert.That(snap.Data.ContainsKey(10), Is.True);
        }

        [Test]
        public void Execute_TickAndTimestamp_Preserved()
        {
            // Arrange
            var playerIds = CreatePlayerArrays(1, out var activeArray);
            var activePlayers = new PackedArray<int>(activeArray, activeArray.Length);

            var timestamp = new DateTime(2026, 2, 13, 10, 30, 0, DateTimeKind.Utc);
            var data = new Dictionary<int, byte[]> { [10] = new byte[] { 1 } };
            var fullSnapshot = new ElympicsSnapshot(
                12345,
                timestamp,
                new FactoryState(new Dictionary<int, FactoryPartState>()),
                data,
                null);

            var scheduled = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            scheduled.Append(0, 0);

            var denseToSparse = new int[128];
            denseToSparse[0] = 10;

            var outputSnapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>();

            // Act
            SnapshotEncoderSystem.Execute(fullSnapshot, playerIds, activePlayers, scheduled, denseToSparse, outputSnapshots);

            // Assert
            var snap = outputSnapshots[ElympicsPlayer.FromIndex(0)];
            Assert.That(snap.Tick, Is.EqualTo(12345));
            Assert.That(snap.TickStartUtc, Is.EqualTo(timestamp));
        }

        [Test]
        public void Execute_FactoryState_Preserved()
        {
            // Arrange
            var playerIds = CreatePlayerArrays(1, out var activeArray);
            var activePlayers = new PackedArray<int>(activeArray, activeArray.Length);

            var factoryState = new FactoryState(new Dictionary<int, FactoryPartState>
            {
                [42] = new FactoryPartState(2, new DynamicElympicsBehaviourInstancesDataState(1, new Dictionary<int, DynamicElympicsBehaviourInstanceData>()))
            });

            var data = new Dictionary<int, byte[]> { [10] = new byte[] { 1 } };
            var fullSnapshot = new ElympicsSnapshot(
                100,
                DateTime.UtcNow,
                factoryState,
                data,
                null);

            var scheduled = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            scheduled.Append(0, 0);

            var denseToSparse = new int[128];
            denseToSparse[0] = 10;

            var outputSnapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>();

            // Act
            SnapshotEncoderSystem.Execute(fullSnapshot, playerIds, activePlayers, scheduled, denseToSparse, outputSnapshots);

            // Assert
            var snap = outputSnapshots[ElympicsPlayer.FromIndex(0)];
            Assert.That(snap.Factory, Is.EqualTo(factoryState)); // Same reference
        }

        [Test]
        public void Execute_OnlyActivePlayersGetSnapshots()
        {
            // Arrange - 4 player capacity, only players 0 and 2 active
            var playerIds = new ElympicsPlayer[4];
            playerIds[0] = ElympicsPlayer.FromIndex(0);
            playerIds[2] = ElympicsPlayer.FromIndex(2);

            var activePlayers = new PackedArray<int>(new int[] { 0, 2, 0, 0 }, 2);

            var fullSnapshot = CreateSnapshot(100, (10, new byte[] { 1 }));

            var scheduled = new PackedArray2D<int>(CreateJagged(4, 128), new int[4]);
            scheduled.Append(0, 0); // networkId 10
            scheduled.Append(2, 0);

            var denseToSparse = new int[128];
            denseToSparse[0] = 10;

            var outputSnapshots = new Dictionary<ElympicsPlayer, ElympicsSnapshot>();

            // Act
            SnapshotEncoderSystem.Execute(fullSnapshot, playerIds, activePlayers, scheduled, denseToSparse, outputSnapshots);

            // Assert - only 2 players get output
            Assert.That(outputSnapshots.Count, Is.EqualTo(2));
            Assert.That(outputSnapshots.ContainsKey(ElympicsPlayer.FromIndex(0)), Is.True);
            Assert.That(outputSnapshots.ContainsKey(ElympicsPlayer.FromIndex(2)), Is.True);
        }
    }
}
