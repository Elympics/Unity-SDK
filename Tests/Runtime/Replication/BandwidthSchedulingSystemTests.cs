using Elympics.Replication;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Replication
{
    [TestFixture]
    [Category("Replication")]
    public class BandwidthSchedulingSystemTests
    {
        #region Helper Methods

        private static PackedArray<int> CreateActivePlayers(params int[] playerIndices)
        {
            return new PackedArray<int>(playerIndices, playerIndices.Length);
        }

        private static int[][] CreateJagged(int rows, int cols)
        {
            var arr = new int[rows][];
            for (var i = 0; i < rows; i++)
                arr[i] = new int[cols];
            return arr;
        }

        #endregion

        // =====================================================================
        // Category 1: Basic Scheduling
        // =====================================================================

        [Test]
        public void Execute_CopiesAllDirtyToScheduled()
        {
            // Arrange
            var dirtySorted = new PackedArray2D<int>(CreateJagged(2, 128), new int[2]);
            dirtySorted.Append(0, 5);
            dirtySorted.Append(0, 10);
            dirtySorted.Append(0, 15);
            dirtySorted.Append(1, 20);

            var scheduled = new PackedArray2D<int>(CreateJagged(2, 128), new int[2]);
            var activePlayers = CreateActivePlayers(0, 1);

            // Act
            BandwidthSchedulingSystem.Execute(activePlayers, dirtySorted, scheduled);

            // Assert
            Assert.That(scheduled.RowCount(0), Is.EqualTo(3));
            Assert.That(scheduled[0, 0], Is.EqualTo(5));
            Assert.That(scheduled[0, 1], Is.EqualTo(10));
            Assert.That(scheduled[0, 2], Is.EqualTo(15));

            Assert.That(scheduled.RowCount(1), Is.EqualTo(1));
            Assert.That(scheduled[1, 0], Is.EqualTo(20));
        }

        [Test]
        public void Execute_ZeroDirty_ZeroScheduled()
        {
            // Arrange
            var dirtySorted = new PackedArray2D<int>(CreateJagged(2, 128), new int[2]); // empty rows
            var scheduled = new PackedArray2D<int>(CreateJagged(2, 128), new int[2]);
            var activePlayers = CreateActivePlayers(0, 1);

            // Act
            BandwidthSchedulingSystem.Execute(activePlayers, dirtySorted, scheduled);

            // Assert
            Assert.That(scheduled.RowCount(0), Is.EqualTo(0));
            Assert.That(scheduled.RowCount(1), Is.EqualTo(0));
        }
    }
}
