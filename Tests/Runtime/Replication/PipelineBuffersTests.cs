using Elympics.Replication;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Replication
{
    [TestFixture]
    [Category("Replication")]
    public class PipelineBuffersTests
    {
        private PipelineBuffers _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = null;
        }

        [TearDown]
        public void TearDown()
        {
            _sut?.Dispose();
        }

        // =====================================================================
        // Category 1: Construction
        // =====================================================================

        [Test]
        public void Construct_InitializesArraysWithCorrectDimensions()
        {
            // Arrange & Act
            _sut = new PipelineBuffers(4, 128);

            // Assert - per-player (row) dimension = maxPlayers, per-entity (column) dimension = denseCapacity
            Assert.That(_sut.RelevantEntities.Length, Is.EqualTo(4));
            Assert.That(_sut.RelevantEntities[0].Length, Is.EqualTo(128));

            Assert.That(_sut.DirtySorted.Length, Is.EqualTo(4));
            Assert.That(_sut.DirtySorted[0].Length, Is.EqualTo(128));

            Assert.That(_sut.Scheduled.Length, Is.EqualTo(4));
            Assert.That(_sut.Scheduled[0].Length, Is.EqualTo(128));

            Assert.That(_sut.OutputSnapshots, Is.Not.Null);
            Assert.That(_sut.OutputSnapshots.Count, Is.EqualTo(0));

            Assert.That(_sut.LastSentTick.Length, Is.EqualTo(4));   // maxPlayers
            Assert.That(_sut.LastSentTick[0].Length, Is.EqualTo(128)); // denseCapacity
        }

        [Test]
        public void Construct_AllRowCountsAreZero()
        {
            // Arrange & Act
            _sut = new PipelineBuffers(4, 128);

            // Assert - all rows start with count 0
            for (var p = 0; p < 4; p++)
            {
                Assert.That(_sut.RelevantCounts[p], Is.EqualTo(0));
                Assert.That(_sut.DirtySortedCounts[p], Is.EqualTo(0));
                Assert.That(_sut.ScheduledCounts[p], Is.EqualTo(0));
            }
        }

        // =====================================================================
        // Category 2: Clear
        // =====================================================================

        [Test]
        public void Clear_PreservesLastSentTickData()
        {
            // Arrange
            _sut = new PipelineBuffers(4, 128);
            _sut.LastSentTick[0][5] = 42;

            // Act
            _sut.Clear();

            // Assert - LastSentTick is persistent, not cleared
            Assert.That(_sut.LastSentTick[0][5], Is.EqualTo(42));
        }

        [Test]
        public void Clear_ResetsRowCountsAndOutputSnapshots()
        {
            // Arrange
            _sut = new PipelineBuffers(4, 128);

            var relevantEntities = new PackedArray2D<int>(_sut.RelevantEntities, _sut.RelevantCounts);
            var dirtySorted = new PackedArray2D<int>(_sut.DirtySorted, _sut.DirtySortedCounts);
            var scheduled = new PackedArray2D<int>(_sut.Scheduled, _sut.ScheduledCounts);

            relevantEntities.Append(0, 7);
            dirtySorted.Append(1, 3);
            scheduled.Append(2, 9);
            _sut.OutputSnapshots[ElympicsPlayer.FromIndex(0)] = null;

            // Act
            _sut.Clear();

            // Assert - row counts reset to zero
            Assert.That(_sut.RelevantCounts[0], Is.EqualTo(0));
            Assert.That(_sut.DirtySortedCounts[1], Is.EqualTo(0));
            Assert.That(_sut.ScheduledCounts[2], Is.EqualTo(0));

            // OutputSnapshots cleared
            Assert.That(_sut.OutputSnapshots.Count, Is.EqualTo(0));
        }

        [Test]
        public void Clear_DoesNotReallocateBackingArrays()
        {
            // Arrange
            _sut = new PipelineBuffers(4, 128);
            var originalRelevantBacking = _sut.RelevantEntities;
            var originalDirtySortedBacking = _sut.DirtySorted;
            var originalScheduledBacking = _sut.Scheduled;

            // Act
            _sut.Clear();

            // Assert - same backing array object references
            Assert.That(_sut.RelevantEntities, Is.SameAs(originalRelevantBacking));
            Assert.That(_sut.DirtySorted, Is.SameAs(originalDirtySortedBacking));
            Assert.That(_sut.Scheduled, Is.SameAs(originalScheduledBacking));
        }

        // =====================================================================
        // Category 3: ResizeDenseDimension
        // =====================================================================

        [Test]
        public void ResizeDenseDimension_ExpandsColumnDimension()
        {
            // Arrange
            _sut = new PipelineBuffers(4, 128);

            // Act
            _sut.ResizeDenseDimension(256);

            // Assert - row dimension unchanged, column dimension resized
            Assert.That(_sut.RelevantEntities.Length, Is.EqualTo(4));
            Assert.That(_sut.RelevantEntities[0].Length, Is.EqualTo(256));

            Assert.That(_sut.DirtySorted[0].Length, Is.EqualTo(256));
            Assert.That(_sut.Scheduled[0].Length, Is.EqualTo(256));
            Assert.That(_sut.LastSentTick[0].Length, Is.EqualTo(256));
        }

        [Test]
        public void ResizeDenseDimension_Shrink_Works()
        {
            // Arrange
            _sut = new PipelineBuffers(4, 128);

            // Act - shrink to 64
            _sut.ResizeDenseDimension(64);

            // Assert
            Assert.That(_sut.RelevantEntities[0].Length, Is.EqualTo(64));
            Assert.That(_sut.DirtySorted[0].Length, Is.EqualTo(64));
            Assert.That(_sut.Scheduled[0].Length, Is.EqualTo(64));
            Assert.That(_sut.LastSentTick[0].Length, Is.EqualTo(64));
        }

        [Test]
        public void ResizeDenseDimension_PreservesLastSentTickData()
        {
            // Arrange
            _sut = new PipelineBuffers(4, 128);
            _sut.LastSentTick[0][10] = 99;
            _sut.LastSentTick[2][50] = 200;

            // Act
            _sut.ResizeDenseDimension(256);

            // Assert - data within old bounds is preserved after resize
            Assert.That(_sut.LastSentTick[0][10], Is.EqualTo(99));
            Assert.That(_sut.LastSentTick[2][50], Is.EqualTo(200));
        }

        // =====================================================================
        // Category 4: Shutdown
        // =====================================================================

        [Test]
        public void Shutdown_NullsReferenceTypeFields()
        {
            // Arrange
            _sut = new PipelineBuffers(4, 128);

            // Act
            _sut.Dispose();

            // Assert - reference-type fields should be null
            Assert.That(_sut.OutputSnapshots, Is.Null);
            Assert.That(_sut.LastSentTick, Is.Null);
            Assert.That(_sut.RelevantEntities, Is.Null);
            Assert.That(_sut.DirtySorted, Is.Null);
            Assert.That(_sut.Scheduled, Is.Null);
        }

        // =====================================================================
        // Category 5: SwapRemoveDenseSlot
        // =====================================================================

        [Test]
        public void SwapRemoveDenseSlot_SwapsLastIntoHole()
        {
            // Arrange
            _sut = new PipelineBuffers(2, 128);

            // "last" slot being moved into the hole
            _sut.LastSentTick[0][3] = 100;
            _sut.LastSentTick[1][3] = 200;

            // "hole" slot being overwritten
            _sut.LastSentTick[0][1] = 50;
            _sut.LastSentTick[1][1] = 60;

            // Act - swap slot 3 (last) into slot 1 (hole)
            _sut.SwapRemoveDenseSlot(1, 3);

            // Assert - hole now contains data from last slot
            Assert.That(_sut.LastSentTick[0][1], Is.EqualTo(100));
            Assert.That(_sut.LastSentTick[1][1], Is.EqualTo(200));

            // Assert - last slot is zeroed
            Assert.That(_sut.LastSentTick[0][3], Is.EqualTo(0));
            Assert.That(_sut.LastSentTick[1][3], Is.EqualTo(0));
        }

        [Test]
        public void SwapRemoveDenseSlot_RemoveLastElement_JustZeros()
        {
            // Arrange
            _sut = new PipelineBuffers(2, 128);
            _sut.LastSentTick[0][5] = 77;
            _sut.LastSentTick[1][5] = 88;

            // Act - removing the last element (denseIndex == lastDenseIndex)
            _sut.SwapRemoveDenseSlot(5, 5);

            // Assert - slot is zeroed, no copy occurred
            Assert.That(_sut.LastSentTick[0][5], Is.EqualTo(0));
            Assert.That(_sut.LastSentTick[1][5], Is.EqualTo(0));
        }

        // =====================================================================
        // Category 6: PackedArray2D Append and RowCount integration
        // =====================================================================

        [Test]
        public void RelevantEntities_Append_IncreasesRowCount()
        {
            // Arrange
            _sut = new PipelineBuffers(4, 128);
            var relevantEntities = new PackedArray2D<int>(_sut.RelevantEntities, _sut.RelevantCounts);

            // Act
            relevantEntities.Append(0, 5);
            relevantEntities.Append(0, 10);
            relevantEntities.Append(1, 3);

            // Assert
            Assert.That(_sut.RelevantCounts[0], Is.EqualTo(2));
            Assert.That(_sut.RelevantCounts[1], Is.EqualTo(1));
            Assert.That(_sut.RelevantCounts[2], Is.EqualTo(0));
        }
    }
}
