using System.Collections.Generic;
using Elympics.Replication;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Replication
{
    [TestFixture]
    [Category("Replication")]
    public class ChangeDetectionSystemTests
    {
        private const long CurrentTick = 100;

        #region Helper Methods

        private static int[] CreateSparseToDense(params (int networkId, int denseIndex)[] mappings)
        {
            var array = new int[256];
            for (var i = 0; i < array.Length; i++)
                array[i] = -1;

            foreach (var (networkId, denseIndex) in mappings)
                array[networkId] = denseIndex;

            return array;
        }

        #endregion

        // =====================================================================
        // Category 1: Change Detection
        // =====================================================================

        [Test]
        public void Execute_NewEntity_StampsLastModifiedTick()
        {
            // Arrange
            var currentData = new Dictionary<int, byte[]> { [10] = new byte[] { 1, 2, 3 } };
            var previousData = new Dictionary<int, byte[]>();
            var lastModifiedTick = new long[10];
            var sparseToDense = CreateSparseToDense((10, 0));

            // Act
            ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense);

            // Assert
            Assert.That(lastModifiedTick[0], Is.EqualTo(CurrentTick));
        }

        [Test]
        public void Execute_NewEntity_No_Previous_Data()
        {
            // Arrange
            var currentData = new Dictionary<int, byte[]> { [10] = new byte[] { 1, 2, 3 } };
            Dictionary<int, byte[]> previousData = null;
            var lastModifiedTick = new long[10];
            var sparseToDense = CreateSparseToDense((10, 0));

            // Act
            ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense);

            // Assert
            Assert.That(lastModifiedTick[0], Is.EqualTo(CurrentTick));
        }

        [Test]
        public void Execute_IdenticalBytes_NotStamped()
        {
            // Arrange
            var currentData = new Dictionary<int, byte[]> { [10] = new byte[] { 1, 2, 3 } };
            var previousData = new Dictionary<int, byte[]> { [10] = new byte[] { 1, 2, 3 } };
            var lastModifiedTick = new long[10];
            var sparseToDense = CreateSparseToDense((10, 0));

            // Act
            ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense);

            // Assert
            Assert.That(lastModifiedTick[0], Is.EqualTo(0)); // Not stamped
        }

        [Test]
        public void Execute_DifferentBytes_Stamped()
        {
            // Arrange
            var currentData = new Dictionary<int, byte[]> { [10] = new byte[] { 1, 2, 3 } };
            var previousData = new Dictionary<int, byte[]> { [10] = new byte[] { 1, 2, 4 } };
            var lastModifiedTick = new long[10];
            var sparseToDense = CreateSparseToDense((10, 0));

            // Act
            ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense);

            // Assert
            Assert.That(lastModifiedTick[0], Is.EqualTo(CurrentTick));
        }

        [Test]
        public void Execute_NullCurrentData_NoOp()
        {
            // Arrange
            Dictionary<int, byte[]> currentData = null;
            var previousData = new Dictionary<int, byte[]>();
            var lastModifiedTick = new long[10];
            var sparseToDense = CreateSparseToDense();

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() =>
                ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense));
        }

        [Test]
        public void Execute_UnregisteredEntity_Skipped()
        {
            // Arrange
            var currentData = new Dictionary<int, byte[]> { [10] = new byte[] { 1, 2, 3 } };
            var previousData = new Dictionary<int, byte[]>();
            var lastModifiedTick = new long[10];
            var sparseToDense = CreateSparseToDense(); // networkId 10 not registered

            // Act
            ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense);

            // Assert - no crash, nothing stamped
            Assert.That(lastModifiedTick[0], Is.EqualTo(0));
        }

        [Test]
        public void Execute_NullCurrentBytes_Stamped()
        {
            // Arrange
            var currentData = new Dictionary<int, byte[]> { [10] = null };
            var previousData = new Dictionary<int, byte[]> { [10] = new byte[] { 1, 2, 3 } };
            var lastModifiedTick = new long[10];
            var sparseToDense = CreateSparseToDense((10, 0));

            // Act
            ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense);

            // Assert
            Assert.That(lastModifiedTick[0], Is.EqualTo(CurrentTick));
        }

        [Test]
        public void Execute_NullPreviousBytes_Stamped()
        {
            // Arrange
            var currentData = new Dictionary<int, byte[]> { [10] = new byte[] { 1, 2, 3 } };
            var previousData = new Dictionary<int, byte[]> { [10] = null };
            var lastModifiedTick = new long[10];
            var sparseToDense = CreateSparseToDense((10, 0));

            // Act
            ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense);

            // Assert
            Assert.That(lastModifiedTick[0], Is.EqualTo(CurrentTick));
        }

        [Test]
        public void Execute_BothCurrentAndPreviousBytesNull_NotStamped()
        {
            // Arrange
            var currentData = new Dictionary<int, byte[]> { [10] = null };
            var previousData = new Dictionary<int, byte[]> { [10] = null };
            var lastModifiedTick = new long[10];
            lastModifiedTick[0] = 0; // Explicitly set to 0
            var sparseToDense = CreateSparseToDense((10, 0));

            // Act
            ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense);

            // Assert - both null means no change, not stamped
            Assert.That(lastModifiedTick[0], Is.EqualTo(0));
        }

        [Test]
        public void Execute_EntityMissingFromPreviousData_Stamped()
        {
            // Arrange
            var currentData = new Dictionary<int, byte[]> { [10] = new byte[] { 1, 2, 3 } };
            var previousData = new Dictionary<int, byte[]>(); // No entry for 10
            var lastModifiedTick = new long[10];
            var sparseToDense = CreateSparseToDense((10, 0));

            // Act
            ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense);

            // Assert
            Assert.That(lastModifiedTick[0], Is.EqualTo(CurrentTick));
        }

        [Test]
        public void Execute_MultipleEntities_MixedChanges_CorrectStamps()
        {
            // Arrange
            var currentData = new Dictionary<int, byte[]>
            {
                [10] = new byte[] { 1, 2, 3 }, // Changed
                [20] = new byte[] { 4, 5, 6 }, // Unchanged
                [30] = new byte[] { 7, 8, 9 }  // New
            };
            var previousData = new Dictionary<int, byte[]>
            {
                [10] = new byte[] { 1, 2, 4 }, // Different
                [20] = new byte[] { 4, 5, 6 }  // Same
            };
            var lastModifiedTick = new long[10];
            var sparseToDense = CreateSparseToDense((10, 0), (20, 1), (30, 2));

            // Act
            ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense);

            // Assert
            Assert.That(lastModifiedTick[0], Is.EqualTo(CurrentTick)); // Changed
            Assert.That(lastModifiedTick[1], Is.EqualTo(0));           // Unchanged
            Assert.That(lastModifiedTick[2], Is.EqualTo(CurrentTick)); // New
        }

        [Test]
        public void Execute_DifferentLengthArrays_Stamped()
        {
            // Arrange
            var currentData = new Dictionary<int, byte[]> { [10] = new byte[] { 1, 2, 3 } };
            var previousData = new Dictionary<int, byte[]> { [10] = new byte[] { 1, 2 } };
            var lastModifiedTick = new long[10];
            var sparseToDense = CreateSparseToDense((10, 0));

            // Act
            ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense);

            // Assert
            Assert.That(lastModifiedTick[0], Is.EqualTo(CurrentTick));
        }

        [Test]
        public void Execute_EmptyByteArrays_NotStamped()
        {
            // Arrange
            var currentData = new Dictionary<int, byte[]> { [10] = new byte[0] };
            var previousData = new Dictionary<int, byte[]> { [10] = new byte[0] };
            var lastModifiedTick = new long[10];
            var sparseToDense = CreateSparseToDense((10, 0));

            // Act
            ChangeDetectionSystem.Execute(currentData, previousData, CurrentTick, lastModifiedTick, sparseToDense);

            // Assert
            Assert.That(lastModifiedTick[0], Is.EqualTo(0)); // Not stamped
        }
    }
}
