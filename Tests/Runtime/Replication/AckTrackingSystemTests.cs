using Elympics.Replication;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Replication
{
    [TestFixture]
    [Category("Replication")]
    public class AckTrackingSystemTests
    {
        #region Helper Methods

        private static PackedArray<int> CreateActivePlayers(params int[] playerIndices) =>
                new(playerIndices, playerIndices.Length);

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
        // Category 1: Stamping Scheduled Entities
        // =====================================================================

        [Test]
        public void Execute_StampsScheduledEntities()
        {
            // Arrange
            var lastSentTick = CreateJaggedLong(2, 128);
            var scheduledData = CreateJagged(2, 128);
            var scheduledCounts = new int[2];
            scheduledData[0][0] = 5;
            scheduledData[0][1] = 10;
            scheduledCounts[0] = 2;
            scheduledData[1][0] = 3;
            scheduledCounts[1] = 1;
            var scheduled = new PackedArray2D<int>(scheduledData, scheduledCounts);

            var activePlayers = CreateActivePlayers(0, 1);
            const long currentTick = 42;

            // Act
            AckTrackingSystem.Execute(activePlayers, scheduled, lastSentTick, currentTick);

            // Assert
            Assert.That(lastSentTick[0][5], Is.EqualTo(42));
            Assert.That(lastSentTick[0][10], Is.EqualTo(42));
            Assert.That(lastSentTick[1][3], Is.EqualTo(42));
        }

        [Test]
        public void Execute_DoesNotStampUnscheduledEntities()
        {
            // Arrange
            var lastSentTick = CreateJaggedLong(2, 128);
            var scheduledData = CreateJagged(2, 128);
            var scheduledCounts = new int[2];
            scheduledData[0][0] = 5;
            scheduledData[0][1] = 10;
            scheduledCounts[0] = 2;
            scheduledData[1][0] = 3;
            scheduledCounts[1] = 1;
            var scheduled = new PackedArray2D<int>(scheduledData, scheduledCounts);

            // Entity at dense index 7 is NOT in scheduled for player 0
            lastSentTick[0][7] = 10;

            var activePlayers = CreateActivePlayers(0, 1);
            const long currentTick = 42;

            // Act
            AckTrackingSystem.Execute(activePlayers, scheduled, lastSentTick, currentTick);

            // Assert - unscheduled entity remains unchanged
            Assert.That(lastSentTick[0][7], Is.EqualTo(10));
        }

        [Test]
        public void Execute_InactivePlayersNotProcessed()
        {
            // Arrange - 4-player capacity, only players 0 and 2 active
            var lastSentTick = CreateJaggedLong(4, 128);
            var scheduledData = CreateJagged(4, 128);
            var scheduledCounts = new int[4];

            // Schedule one entity for all four player slots
            for (var p = 0; p < 4; p++)
            {
                scheduledData[p][0] = 0;
                scheduledCounts[p] = 1;
            }
            var scheduled = new PackedArray2D<int>(scheduledData, scheduledCounts);

            var activeArray = new int[] { 0, 2 };
            var activePlayers = new PackedArray<int>(activeArray, 2);

            const long currentTick = 42;

            // Act
            AckTrackingSystem.Execute(activePlayers, scheduled, lastSentTick, currentTick);

            // Assert - players 0 and 2 stamped; players 1 and 3 left at 0
            Assert.That(lastSentTick[0][0], Is.EqualTo(42));
            Assert.That(lastSentTick[2][0], Is.EqualTo(42));
            Assert.That(lastSentTick[1][0], Is.EqualTo(0));
            Assert.That(lastSentTick[3][0], Is.EqualTo(0));
        }

        [Test]
        public void Execute_StampValueEqualsCurrentTick()
        {
            // Arrange - single player, single entity at dense index 0
            var lastSentTick = CreateJaggedLong(1, 128);
            lastSentTick[0][0] = 99; // Old value

            var scheduledData = CreateJagged(1, 128);
            var scheduledCounts = new int[1];
            scheduledData[0][0] = 0;
            scheduledCounts[0] = 1;
            var scheduled = new PackedArray2D<int>(scheduledData, scheduledCounts);

            var activePlayers = CreateActivePlayers(0);
            const long currentTick = 500;

            // Act
            AckTrackingSystem.Execute(activePlayers, scheduled, lastSentTick, currentTick);

            // Assert - old value replaced with currentTick exactly
            Assert.That(lastSentTick[0][0], Is.EqualTo(500));
        }
    }
}
