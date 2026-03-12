using Elympics.Replication;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Replication
{
    [TestFixture]
    [Category("Replication")]
    public class PrioritizationSystemTests
    {
        #region Helper Methods

        /// <summary>
        /// Creates SOA player arrays suitable for PrioritizationSystem.Execute.
        /// All players at indices 0..count-1 are activated.
        /// </summary>
        private static long[] CreatePlayerArrays(
            int count,
            out int[] activePlayerArray,
            params long[] lastReceivedSnapshots)
        {
            var lastRecv = new long[count];
            activePlayerArray = new int[count];
            for (var i = 0; i < count; i++)
            {
                lastRecv[i] = i < lastReceivedSnapshots.Length ? lastReceivedSnapshots[i] : -1;
                activePlayerArray[i] = i;
            }

            return lastRecv;
        }

        /// <summary>
        /// Creates a PackedArray2D with pre-filled values (indexed [row][col]) and row counts.
        /// </summary>
        private static PackedArray2D<int> CreateFilledBuffer(int rows, int cols, int[][] values, int[] rowCounts)
        {
            var backingArray = CreateJagged(rows, cols);
            var counts = new int[rows];
            for (var row = 0; row < rows; row++)
            {
                counts[row] = rowCounts[row];
                for (var col = 0; col < rowCounts[row]; col++)
                    backingArray[row][col] = values[row][col];
            }

            return new PackedArray2D<int>(backingArray, counts);
        }

        /// <summary>
        /// Overload that pre-fills lastSentTick[p][d] = playerLastReceivedSnapshot[p] for all (p, d).
        /// Used by existing tests to reproduce the old "dirty = lastModifiedTick > lastRecv" behaviour
        /// under the new two-condition ack logic.
        /// </summary>
        private static (long[][] lastSentTick, int[] netUpdateInterval) CreateDefaultAckArrays(
            int maxPlayers,
            int denseCapacity,
            long[] playerLastReceivedSnapshot)
        {
            var lastSentTick = CreateJaggedLong(maxPlayers, denseCapacity);
            var netUpdateInterval = new int[denseCapacity];
            for (var d = 0; d < denseCapacity; d++)
                netUpdateInterval[d] = 1;

            // Setting lastSentTick[p][d] = lastRecv[p] means:
            //   sentSinceChange = (lastRecv >= lastModifiedTick)  — exactly the old dirty check
            //   clientAckedSend = (lastRecv >= lastRecv) = true   — always acked
            // So skip iff lastRecv >= lastModifiedTick, which matches the previous system.
            for (var p = 0; p < maxPlayers; p++)
            {
                var lastRecv = p < playerLastReceivedSnapshot.Length ? playerLastReceivedSnapshot[p] : -1L;
                if (lastRecv < 0)
                    continue; // cold-start players: lastSentTick stays 0, cold-start path fires anyway
                for (var d = 0; d < denseCapacity; d++)
                    lastSentTick[p][d] = lastRecv;
            }

            return (lastSentTick, netUpdateInterval);
        }

        private static int[][] CreateJagged(int rows, int cols)
        {
            var arr = new int[rows][];
            for (var i = 0; i < rows; i++)
                arr[i] = new int[cols];
            return arr;
        }

        private static long[][] CreateJaggedLong(int rows, int cols)
        {
            var arr = new long[rows][];
            for (var i = 0; i < rows; i++)
                arr[i] = new long[cols];
            return arr;
        }

        #endregion

        // =====================================================================
        // Category 1: Cold Start vs Warm Players
        // =====================================================================

        [Test]
        public void Execute_ColdStartPlayer_GetsEverything_WarmPlayerFilters()
        {
            // Arrange
            var lastRecv = CreatePlayerArrays(2, out var activePlayerArray, -1, 50);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);

            var lastModifiedTick = new long[3];
            lastModifiedTick[0] = 40;
            lastModifiedTick[1] = 60;
            lastModifiedTick[2] = 70;

            var relevantEntities = new PackedArray2D<int>(CreateJagged(2, 128), new int[2]);
            relevantEntities.Append(0, 0);
            relevantEntities.Append(0, 1);
            relevantEntities.Append(0, 2);
            relevantEntities.Append(1, 0);
            relevantEntities.Append(1, 1);
            relevantEntities.Append(1, 2);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(2, 128), new int[2]);

            var (lastSentTick, netUpdateInterval) = CreateDefaultAckArrays(2, 128, lastRecv);

            // Act
            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                0L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(3)); // Cold start: all 3
            Assert.That(dirtySorted.RowCount(1), Is.EqualTo(2)); // Warm: only 2 (modified after 50)
        }

        [Test]
        public void Execute_WarmPlayer_OnlyGetsModifiedEntities()
        {
            // Arrange
            var lastRecv = CreatePlayerArrays(1, out var activePlayerArray, 100);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);

            var lastModifiedTick = new long[3];
            lastModifiedTick[0] = 90; // Not dirty
            lastModifiedTick[1] = 110; // Dirty
            lastModifiedTick[2] = 150; // Dirty

            var relevantEntities = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            relevantEntities.Append(0, 0);
            relevantEntities.Append(0, 1);
            relevantEntities.Append(0, 2);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);

            var (lastSentTick, netUpdateInterval) = CreateDefaultAckArrays(1, 128, lastRecv);

            // Act
            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                0L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(2)); // Only 2 dirty
            Assert.That(dirtySorted[0, 0], Is.EqualTo(1)); // Dense index 1
            Assert.That(dirtySorted[0, 1], Is.EqualTo(2)); // Dense index 2
        }

        [Test]
        public void Execute_NoRelevantEntities_ZeroDirty()
        {
            // Arrange
            var lastRecv = CreatePlayerArrays(1, out var activePlayerArray, -1);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);
            var lastModifiedTick = new long[10];

            var relevantEntities = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]); // empty row
            var dirtySorted = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);

            var (lastSentTick, netUpdateInterval) = CreateDefaultAckArrays(1, 128, lastRecv);

            // Act
            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                0L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(0));
        }

        [Test]
        public void Execute_AllEntitiesClean_ZeroDirty()
        {
            // Arrange
            var lastRecv = CreatePlayerArrays(1, out var activePlayerArray, 100);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);

            var lastModifiedTick = new long[3];
            lastModifiedTick[0] = 50;
            lastModifiedTick[1] = 60;
            lastModifiedTick[2] = 70;

            var relevantEntities = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            relevantEntities.Append(0, 0);
            relevantEntities.Append(0, 1);
            relevantEntities.Append(0, 2);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);

            var (lastSentTick, netUpdateInterval) = CreateDefaultAckArrays(1, 128, lastRecv);

            // Act
            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                0L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(0)); // All clean
        }

        [Test]
        public void Execute_MixedPlayers_IndependentFiltering()
        {
            // Arrange
            var lastRecv = CreatePlayerArrays(3, out var activePlayerArray, -1, 50, 100);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);

            var lastModifiedTick = new long[4];
            lastModifiedTick[0] = 40;
            lastModifiedTick[1] = 60;
            lastModifiedTick[2] = 80;
            lastModifiedTick[3] = 110;

            var relevantEntities = new PackedArray2D<int>(CreateJagged(3, 128), new int[3]);
            for (var p = 0; p < 3; p++)
                for (var i = 0; i < 4; i++)
                    relevantEntities.Append(p, i);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(3, 128), new int[3]);

            var (lastSentTick, netUpdateInterval) = CreateDefaultAckArrays(3, 128, lastRecv);

            // Act
            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                0L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(4)); // Cold start: all 4
            Assert.That(dirtySorted.RowCount(1), Is.EqualTo(3)); // Modified after 50: 3 entities
            Assert.That(dirtySorted.RowCount(2), Is.EqualTo(1)); // Modified after 100: 1 entity
        }

        [Test]
        public void Execute_LastRecvEqualsLastModified_NotDirty()
        {
            // Arrange
            var lastRecv = CreatePlayerArrays(1, out var activePlayerArray, 100);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);

            var lastModifiedTick = new long[1];
            lastModifiedTick[0] = 100; // Equal to LastReceivedSnapshot

            var relevantEntities = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            relevantEntities.Append(0, 0);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);

            var (lastSentTick, netUpdateInterval) = CreateDefaultAckArrays(1, 128, lastRecv);

            // Act
            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                0L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert - boundary: equal means NOT dirty
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(0));
        }

        [Test]
        public void Execute_LastRecvOneBeforeLastModified_IsDirty()
        {
            // Arrange
            var lastRecv = CreatePlayerArrays(1, out var activePlayerArray, 99);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);

            var lastModifiedTick = new long[1];
            lastModifiedTick[0] = 100; // One tick after LastReceivedSnapshot

            var relevantEntities = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            relevantEntities.Append(0, 0);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);

            var (lastSentTick, netUpdateInterval) = CreateDefaultAckArrays(1, 128, lastRecv);

            // Act
            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                0L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert - boundary: one tick after means dirty
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(1));
            Assert.That(dirtySorted[0, 0], Is.EqualTo(0));
        }

        [Test]
        public void Execute_InactivePlayersNotProcessed()
        {
            // Arrange - maxPlayers capacity = 4, but only players 0 and 2 active
            var lastRecv = new long[4];
            lastRecv[0] = -1;
            lastRecv[1] = -1;
            lastRecv[2] = -1;
            lastRecv[3] = -1;

            var activePlayers = new PackedArray<int>(new[] { 0, 2 }, 2);

            var lastModifiedTick = new long[2];
            lastModifiedTick[0] = 10;
            lastModifiedTick[1] = 20;

            var relevantEntities = new PackedArray2D<int>(CreateJagged(4, 128), new int[4]);
            relevantEntities.Append(0, 0);
            relevantEntities.Append(0, 1);
            relevantEntities.Append(2, 0);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(4, 128), new int[4]);

            var (lastSentTick, netUpdateInterval) = CreateDefaultAckArrays(4, 128, lastRecv);

            // Act
            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                0L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert - only players 0 and 2 processed
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(2)); // Player 0: cold start, all 2
            Assert.That(dirtySorted.RowCount(2), Is.EqualTo(1)); // Player 2: cold start, all 1
            // Players 1 and 3 untouched (count stays at 0 from array init)
            Assert.That(dirtySorted.RowCount(1), Is.EqualTo(0));
            Assert.That(dirtySorted.RowCount(3), Is.EqualTo(0));
        }

        // =====================================================================
        // Category 2: Ack-Aware Retransmission Logic
        // =====================================================================

        [Test]
        public void Execute_EntitySentButNotAcked_ResentAfterInterval()
        {
            // Arrange — entity sent at tick 50, but client's last ack is only tick 45 (not yet acked).
            // sentSinceChange=true (50>=40), clientAckedSend=false (45<50) → throttled re-send path.
            // currentTick=65: 65-50=15 >= interval(15) → interval met → include.
            var lastRecv = CreatePlayerArrays(1, out var activePlayerArray, 45);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);

            var lastModifiedTick = new long[1];
            lastModifiedTick[0] = 40; // Entity changed at 40, before last send

            var lastSentTick = CreateJaggedLong(1, 1);
            lastSentTick[0][0] = 50; // Last sent at tick 50; sentSinceChange=true (50>=40)

            var netUpdateInterval = new int[1];
            netUpdateInterval[0] = 15; // Re-send interval: 15 ticks

            var relevantEntities = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            relevantEntities.Append(0, 0);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);

            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                65L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert — re-send fires exactly at boundary tick
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(1));
        }

        [Test]
        public void Execute_EntitySentButNotAcked_ThrottledByInterval()
        {
            // Arrange — same setup as above but currentTick=64 (one tick before interval fires).
            // 64-50=14 < 15 → interval not met → throttle.
            var lastRecv = CreatePlayerArrays(1, out var activePlayerArray, 45);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);

            var lastModifiedTick = new long[1];
            lastModifiedTick[0] = 40;

            var lastSentTick = CreateJaggedLong(1, 1);
            lastSentTick[0][0] = 50;

            var netUpdateInterval = new int[1];
            netUpdateInterval[0] = 15;

            var relevantEntities = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            relevantEntities.Append(0, 0);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);

            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                64L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert — throttled, not included
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(0));
        }

        [Test]
        public void Execute_EntitySentAndAcked_NotDirty()
        {
            // Arrange — entity sent at 50, client acked at 60; change happened at 40
            // sentSinceChange=true (50>=40), clientAckedSend=true (60>=50) → confirmed delivered
            var lastRecv = CreatePlayerArrays(1, out var activePlayerArray, 60);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);

            var lastModifiedTick = new long[1];
            lastModifiedTick[0] = 40;

            var lastSentTick = CreateJaggedLong(1, 1);
            lastSentTick[0][0] = 50;

            var netUpdateInterval = new int[1];
            netUpdateInterval[0] = 15;

            var relevantEntities = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            relevantEntities.Append(0, 0);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);

            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                65L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert — confirmed delivered, skip
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(0));
        }

        [Test]
        public void Execute_EntityChangedAfterLastSend_ImmediatelyDirty()
        {
            // Arrange — entity changed at tick 60, but last sent at tick 50 (before the change)
            // sentSinceChange=false (50<60) → always include, regardless of interval or ack
            var lastRecv = CreatePlayerArrays(1, out var activePlayerArray, 50);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);

            var lastModifiedTick = new long[1];
            lastModifiedTick[0] = 60; // Changed AFTER last send

            var lastSentTick = CreateJaggedLong(1, 1);
            lastSentTick[0][0] = 50;

            var netUpdateInterval = new int[1];
            netUpdateInterval[0] = 15;

            var relevantEntities = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            relevantEntities.Append(0, 0);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);

            // currentTick deliberately low — proves interval is irrelevant for genuinely dirty entities
            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                51L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert — genuinely dirty, immediate inclusion
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(1));
        }

        [Test]
        public void Execute_NeverSentEntity_TreatedAsDirty()
        {
            // Arrange — entity never sent (lastSentTick=0), entity changed at tick 40
            // sentSinceChange = (0 >= 40) = false → genuinely dirty path, always include
            // Simulates a budget-capped entity that was skipped in a previous tick
            var lastRecv = CreatePlayerArrays(1, out var activePlayerArray, 50);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);

            var lastModifiedTick = new long[1];
            lastModifiedTick[0] = 40;

            var lastSentTick = CreateJaggedLong(1, 1);
            lastSentTick[0][0] = 0; // Never sent

            var netUpdateInterval = new int[1];
            netUpdateInterval[0] = 15;

            var relevantEntities = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            relevantEntities.Append(0, 0);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);

            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                65L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert — never-sent entity is treated as dirty, included immediately
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(1));
        }

        [Test]
        public void Execute_DifferentPriorities_ThrottledIndependently()
        {
            // Arrange — 2 entities with different re-send intervals, 1 tick since last send.
            // Both sent at tick 50; client last acked tick 49 (not yet acked either send).
            // sentSinceChange=true for both (50>=40), clientAckedSend=false for both (49<50).
            // Entity 0 (Critical, interval=1):  currentTick - lastSentTick = 51-50 = 1 >= 1 → include
            // Entity 1 (Low, interval=30):      currentTick - lastSentTick = 51-50 = 1 < 30  → exclude
            var lastRecv = CreatePlayerArrays(1, out var activePlayerArray, 49);
            var activePlayers = new PackedArray<int>(activePlayerArray, activePlayerArray.Length);

            var lastModifiedTick = new long[2];
            lastModifiedTick[0] = 40;
            lastModifiedTick[1] = 40;

            var lastSentTick = CreateJaggedLong(1, 2);
            lastSentTick[0][0] = 50; // Both entities last sent at tick 50
            lastSentTick[0][1] = 50;

            var netUpdateInterval = new int[2];
            netUpdateInterval[0] = 1; // Critical: re-send every tick
            netUpdateInterval[1] = 30; // Low: re-send every 30 ticks

            var relevantEntities = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);
            relevantEntities.Append(0, 0);
            relevantEntities.Append(0, 1);

            var dirtySorted = new PackedArray2D<int>(CreateJagged(1, 128), new int[1]);

            // currentTick=51: exactly 1 tick since send
            PrioritizationSystem.Execute(lastRecv,
                activePlayers,
                lastModifiedTick,
                lastSentTick,
                51L,
                netUpdateInterval,
                relevantEntities,
                dirtySorted);

            // Assert — only the Critical entity passes throttle
            Assert.That(dirtySorted.RowCount(0), Is.EqualTo(1));
            Assert.That(dirtySorted[0, 0], Is.EqualTo(0)); // Dense index 0 (Critical entity)
        }
    }
}
