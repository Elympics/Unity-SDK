using Elympics.Replication;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Replication
{
    [TestFixture]
    [Category("Replication")]
    public class PackedArray2DTests
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
        /// Creates a PackedArray2D with a single row populated with the given values.
        /// </summary>
        private static PackedArray2D<int> CreateSingleRow(int capacity, params int[] values)
        {
            var jagged = CreateJagged(1, capacity);
            var rowCounts = new int[1];
            var pa = new PackedArray2D<int>(jagged, rowCounts);
            for (var i = 0; i < values.Length; i++)
                pa.Append(0, values[i]);
            return pa;
        }

        #endregion

        // =====================================================================
        // Remove (swap-remove)
        // =====================================================================

        [Test]
        public void Remove_SingleElement_DecreasesRowCount()
        {
            // Arrange
            var pa = CreateSingleRow(8, 42);
            Assert.That(pa.RowCount(0), Is.EqualTo(1));

            // Act
            pa.Remove(0, 0);

            // Assert
            Assert.That(pa.RowCount(0), Is.EqualTo(0));
        }

        [Test]
        public void Remove_FirstOfThree_SwapsLastIntoSlot()
        {
            // Arrange - row contains [10, 20, 30]
            var pa = CreateSingleRow(8, 10, 20, 30);
            Assert.That(pa.RowCount(0), Is.EqualTo(3));

            // Act - remove index 0 => last element (30) swapped into slot 0
            pa.Remove(0, 0);

            // Assert
            Assert.That(pa.RowCount(0), Is.EqualTo(2));
            Assert.That(pa[0, 0], Is.EqualTo(30)); // 30 moved into slot 0
            Assert.That(pa[0, 1], Is.EqualTo(20)); // 20 unchanged
        }

        [Test]
        public void Remove_LastElement_JustDecrements()
        {
            // Arrange - row contains [10, 20, 30]
            var pa = CreateSingleRow(8, 10, 20, 30);

            // Act - remove the last column index (2) => no swap needed, just decrement
            pa.Remove(0, 2);

            // Assert
            Assert.That(pa.RowCount(0), Is.EqualTo(2));
            Assert.That(pa[0, 0], Is.EqualTo(10));
            Assert.That(pa[0, 1], Is.EqualTo(20));
        }

        [Test]
        public void Remove_MiddleElement_SwapsCorrectly()
        {
            // Arrange - row contains [10, 20, 30, 40]
            var pa = CreateSingleRow(8, 10, 20, 30, 40);
            Assert.That(pa.RowCount(0), Is.EqualTo(4));

            // Act - remove index 1 => last element (40) swapped into slot 1
            pa.Remove(0, 1);

            // Assert
            Assert.That(pa.RowCount(0), Is.EqualTo(3));
            Assert.That(pa[0, 0], Is.EqualTo(10)); // unchanged
            Assert.That(pa[0, 1], Is.EqualTo(40)); // 40 moved from slot 3
            Assert.That(pa[0, 2], Is.EqualTo(30)); // unchanged
        }

        // =====================================================================
        // RowSpan
        // =====================================================================

        [Test]
        public void RowSpan_ReturnsCorrectSlice()
        {
            // Arrange - row contains [5, 10, 15]
            var pa = CreateSingleRow(8, 5, 10, 15);

            // Act
            var span = pa.RowSpan(0);

            // Assert
            Assert.That(span.Length, Is.EqualTo(3));
            Assert.That(span[0], Is.EqualTo(5));
            Assert.That(span[1], Is.EqualTo(10));
            Assert.That(span[2], Is.EqualTo(15));
        }

        [Test]
        public void RowSpan_EmptyRow_ReturnsEmpty()
        {
            // Arrange - empty row
            var jagged = CreateJagged(2, 8);
            var rowCounts = new int[2];
            var pa = new PackedArray2D<int>(jagged, rowCounts);

            // Act
            var span = pa.RowSpan(0);

            // Assert
            Assert.That(span.Length, Is.EqualTo(0));
            Assert.That(span.IsEmpty, Is.True);
        }
    }
}
