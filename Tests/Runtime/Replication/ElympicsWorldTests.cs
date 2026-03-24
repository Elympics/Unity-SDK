using System;
using System.Text.RegularExpressions;
using Elympics.Replication;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Elympics.Tests.Runtime.Replication
{
    [TestFixture]
    [Category("Replication")]
    public class ElympicsWorldTests
    {
        private ElympicsWorld _sut;
        private ReplicationPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            ElympicsWorld.Current = null;
            ReplicationPipeline.Current = null;
            _sut = null;
            _pipeline = null;
        }

        [TearDown]
        public void TearDown()
        {
            _pipeline?.Dispose();
            _sut?.Dispose();
            ElympicsWorld.Current = null;
            ReplicationPipeline.Current = null;
        }

        #region Helper Methods

        private static ElympicsSnapshot CreateSnapshot(long tick, int entityCount)
        {
            var data = new System.Collections.Generic.Dictionary<int, byte[]>();
            for (var i = 0; i < entityCount; i++)
                data[i] = new[] { (byte)i };

            return new ElympicsSnapshot(
                tick,
                DateTime.UtcNow,
                new FactoryState(new System.Collections.Generic.Dictionary<int, FactoryPartState>()),
                data,
                null);
        }

        private static uint CalculateAllPlayersMask(int maxPlayers) => maxPlayers == 32 ? uint.MaxValue : (1u << maxPlayers) - 1;

        #endregion

        // =====================================================================
        // Category 1: Construction
        // =====================================================================

        [Test]
        public void Construct_WithValidParams_InitializesArrays()
        {
            // Arrange & Act
            _sut = new ElympicsWorld(4, 256, 512);

            // Assert
            Assert.That(_sut.SparseToDense.Length, Is.EqualTo(256));
            Assert.That(_sut.DenseCount, Is.EqualTo(0));
            Assert.That(_sut.DenseCapacity, Is.EqualTo(128)); // Initial capacity
            Assert.That(_sut.MaxPlayers, Is.EqualTo(4));
            Assert.That(_sut.ActivePlayersCount, Is.EqualTo(0));

            // All SparseToDense should be -1 (unregistered)
            foreach (var value in _sut.SparseToDense)
                Assert.That(value, Is.EqualTo(-1));
        }

        [Test]
        public void Construct_MaxDenseCapacityLessThanInitial_ClampsToMaxDense()
        {
            // Arrange & Act
            _sut = new ElympicsWorld(2, 64, 50);

            // Assert - should be clamped to maxDenseEntities
            Assert.That(_sut.DenseCapacity, Is.EqualTo(50));
        }

        [Test]
        public void Construct_StoresMaxPlayers()
        {
            // Arrange & Act
            _sut = new ElympicsWorld(8, 256, 512);

            // Assert
            Assert.That(_sut.MaxPlayers, Is.EqualTo(8));
        }

        // =====================================================================
        // Category 1b: ActivatePlayer
        // =====================================================================

        [Test]
        public void ActivatePlayer_AppendsToActivePlayerIndices()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act
            _sut.ActivatePlayer(0, ElympicsPlayer.FromIndex(0));
            _sut.ActivatePlayer(2, ElympicsPlayer.FromIndex(2));

            // Assert
            Assert.That(_sut.ActivePlayersCount, Is.EqualTo(2));
            Assert.That(_sut.ActivePlayers[0], Is.EqualTo(0));
            Assert.That(_sut.ActivePlayers[1], Is.EqualTo(2));
            Assert.That(_sut.PlayerIds[0], Is.EqualTo(ElympicsPlayer.FromIndex(0)));
            Assert.That(_sut.PlayerIds[2], Is.EqualTo(ElympicsPlayer.FromIndex(2)));
            Assert.That(_sut.PlayerLastReceivedSnapshot[0], Is.EqualTo(-1));
            Assert.That(_sut.PlayerLastReceivedSnapshot[2], Is.EqualTo(-1));
        }

        [Test]
        public void ActivatePlayer_DuplicateIndex_LogsWarning()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.ActivatePlayer(1, ElympicsPlayer.FromIndex(1));

            // Act & Assert
            LogAssert.Expect(LogType.Warning, new Regex(@"\[ElympicsWorld\] Player at index 1 already active. Skipping."));
            _sut.ActivatePlayer(1, ElympicsPlayer.FromIndex(1));

            Assert.That(_sut.ActivePlayersCount, Is.EqualTo(1));
        }

        [Test]
        public void ActivatePlayer_OutOfRange_LogsError()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act & Assert
            LogAssert.Expect(LogType.Error, new Regex(@"Cannot activate player at index 5"));
            _sut.ActivatePlayer(5, ElympicsPlayer.FromIndex(5));

            Assert.That(_sut.ActivePlayersCount, Is.EqualTo(0));
        }

        [Test]
        public void ActivatePlayer_NegativeIndex_LogsError()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act & Assert
            LogAssert.Expect(LogType.Error, new Regex(@"Cannot activate player at index -1"));
            _sut.ActivatePlayer(-1, ElympicsPlayer.FromIndex(0));

            Assert.That(_sut.ActivePlayersCount, Is.EqualTo(0));
        }

        // =====================================================================
        // Category 2: RegisterEntity
        // =====================================================================

        [Test]
        public void RegisterEntity_FirstEntity_CreatesMapping()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act
            _sut.RegisterEntity(5, ElympicsPlayer.All);

            // Assert
            Assert.That(_sut.DenseCount, Is.EqualTo(1));
            Assert.That(_sut.SparseToDense[5], Is.EqualTo(0));
            Assert.That(_sut.DenseToSparse[0], Is.EqualTo(5));
            Assert.That(_sut.InterestMask[0], Is.EqualTo(CalculateAllPlayersMask(4))); // 0b1111 = 15
        }

        [Test]
        public void RegisterEntity_MultipleEntities_DifferentVisibility_CorrectMasks()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act
            _sut.RegisterEntity(10, ElympicsPlayer.All);
            _sut.RegisterEntity(20, ElympicsPlayer.FromIndex(2));
            _sut.RegisterEntity(30, ElympicsPlayer.World);

            // Assert
            Assert.That(_sut.DenseCount, Is.EqualTo(3));
            Assert.That(_sut.InterestMask[0], Is.EqualTo(15u)); // All = 0b1111
            Assert.That(_sut.InterestMask[1], Is.EqualTo(4u)); // Player 2 = 1 << 2 = 0b0100
            Assert.That(_sut.InterestMask[2], Is.EqualTo(0u)); // World = 0
        }

        [Test]
        public void RegisterEntity_SameIdTwice_LogsWarning()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.RegisterEntity(5, ElympicsPlayer.All);

            // Act & Assert - warning expected
            LogAssert.Expect(LogType.Warning, new Regex(@"\[ElympicsWorld\] Entity 5 already registered at dense index 0. Skipping."));
            _sut.RegisterEntity(5, ElympicsPlayer.All);

            // Should still have only 1 entity
            Assert.That(_sut.DenseCount, Is.EqualTo(1));
        }

        [Test]
        public void RegisterEntity_BeyondMaxCapacity_LogsError()
        {
            // Arrange - small capacity
            _sut = new ElympicsWorld(4, 256, 2);
            _sut.RegisterEntity(1, ElympicsPlayer.All);
            _sut.RegisterEntity(2, ElympicsPlayer.All);

            // Act & Assert - error expected
            LogAssert.Expect(LogType.Error, new Regex("Cannot register entity"));
            _sut.RegisterEntity(3, ElympicsPlayer.All);

            // Should still have only 2 entities
            Assert.That(_sut.DenseCount, Is.EqualTo(2));
        }

        [Test]
        public void RegisterEntity_WithGenerationalId_IsValidWorks()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            var generationalId = NetworkIdConstants.EncodeNetworkId(5, 42);

            // Act
            _sut.RegisterEntity(generationalId, ElympicsPlayer.All);

            // Assert
            Assert.That(_sut.IsValid(generationalId), Is.True);
            Assert.That(_sut.DenseCount, Is.EqualTo(1));
        }

        [Test]
        public void RegisterEntity_SpecificPlayer_CorrectMask()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act - Register entity visible to player 3 only
            _sut.RegisterEntity(7, ElympicsPlayer.FromIndex(3));

            // Assert
            var expectedMask = 1u << 3; // = 8
            Assert.That(_sut.InterestMask[0], Is.EqualTo(expectedMask));
        }

        // =====================================================================
        // Category 3: UnregisterEntity
        // =====================================================================

        [Test]
        public void UnregisterEntity_LastElement_DecrementsDenseCount()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.RegisterEntity(5, ElympicsPlayer.All);

            // Act
            _sut.UnregisterEntity(5);

            // Assert
            Assert.That(_sut.DenseCount, Is.EqualTo(0));
            Assert.That(_sut.SparseToDense[5], Is.EqualTo(-1));
        }

        [Test]
        public void UnregisterEntity_FirstOfThree_SwapRemove()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.RegisterEntity(10, ElympicsPlayer.All);
            _sut.RegisterEntity(20, ElympicsPlayer.All);
            _sut.RegisterEntity(30, ElympicsPlayer.All);

            // Act - unregister first entity (networkId=10, denseIndex=0)
            _sut.UnregisterEntity(10);

            // Assert - last entity (30) should move to slot 0
            Assert.That(_sut.DenseCount, Is.EqualTo(2));
            Assert.That(_sut.DenseToSparse[0], Is.EqualTo(30)); // Last moved to first
            Assert.That(_sut.SparseToDense[30], Is.EqualTo(0));
            Assert.That(_sut.SparseToDense[10], Is.EqualTo(-1));
        }

        [Test]
        public void UnregisterEntity_NonExistent_LogsWarning()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act & Assert
            LogAssert.Expect(LogType.Warning, new Regex("Entity 100 not registered"));
            _sut.UnregisterEntity(100);
        }


        [Test]
        public void UnregisterEntity_TriggersCapacityShrink()
        {
            // Arrange - register 129 entities to grow capacity to 256
            _sut = new ElympicsWorld(4, 8192, 512);
            for (var i = 0; i < 129; i++)
                _sut.RegisterEntity(i, ElympicsPlayer.All);

            Assert.That(_sut.DenseCapacity, Is.EqualTo(256)); // Grown

            // Act - unregister down to 3/8 threshold (96 entities)
            for (var i = 128; i >= 96; i--)
                _sut.UnregisterEntity(i);

            // Assert - capacity should shrink to 128
            Assert.That(_sut.DenseCount, Is.EqualTo(96));
            Assert.That(_sut.DenseCapacity, Is.EqualTo(128));
        }

        [Test]
        public void UnregisterEntity_AllEntities_CapacityStaysAtFloor()
        {
            // Arrange - start with 128 capacity
            _sut = new ElympicsWorld(4, 256, 512);
            for (var i = 0; i < 128; i++)
                _sut.RegisterEntity(i, ElympicsPlayer.All);

            // Act - unregister all
            for (var i = 0; i < 128; i++)
                _sut.UnregisterEntity(i);

            // Assert - capacity stays at floor (128)
            Assert.That(_sut.DenseCount, Is.EqualTo(0));
            Assert.That(_sut.DenseCapacity, Is.EqualTo(128));
        }

        [Test]
        public void UnregisterEntity_CopiesLastModifiedTickAndInterestMask()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.RegisterEntity(10, ElympicsPlayer.All);
            _sut.RegisterEntity(20, ElympicsPlayer.FromIndex(2));

            // Manually set LastModifiedTick for testing
            _sut.LastModifiedTick[0] = 100;
            _sut.LastModifiedTick[1] = 200;

            // Act - unregister first entity
            _sut.UnregisterEntity(10);

            // Assert - entity 20 moved to slot 0, preserving its data
            Assert.That(_sut.LastModifiedTick[0], Is.EqualTo(200));
            Assert.That(_sut.InterestMask[0], Is.EqualTo(4u)); // Player 2 mask
        }

        [Test]
        public void UnregisterEntity_OnlyEntity_NoCrash()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.RegisterEntity(42, ElympicsPlayer.All);

            // Act & Assert - should not crash
            Assert.DoesNotThrow(() => _sut.UnregisterEntity(42));
            Assert.That(_sut.DenseCount, Is.EqualTo(0));
        }

        // =====================================================================
        // Category 4: GrowDenseArrays
        // =====================================================================

        [Test]
        public void GrowDenseArrays_CapacityDoublesWhenNeeded()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 8192, 512);

            // Act - register 129 entities (beyond initial 128)
            for (var i = 0; i < 129; i++)
                _sut.RegisterEntity(i, ElympicsPlayer.All);

            // Assert - capacity doubled to 256
            Assert.That(_sut.DenseCapacity, Is.EqualTo(256));
            Assert.That(_sut.DenseCount, Is.EqualTo(129));
        }

        [Test]
        public void GrowDenseArrays_ClampedToMaxDenseEntities()
        {
            // Arrange - max is 200
            _sut = new ElympicsWorld(4, 8192, 200);

            // Act - register 129 entities
            for (var i = 0; i < 129; i++)
                _sut.RegisterEntity(i, ElympicsPlayer.All);

            // Assert - capacity grows to max (200), not 256
            Assert.That(_sut.DenseCapacity, Is.EqualTo(200));
        }

        [Test]
        public void GrowDenseArrays_WithPipelineBuffers_CallsResize()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 8192, 512);
            _pipeline = new ReplicationPipeline(4, _sut);

            // Act - register 129 entities to trigger grow
            for (var i = 0; i < 129; i++)
                _sut.RegisterEntity(i, ElympicsPlayer.All);

            // Assert - PipelineBuffers should have resized
            Assert.That(_pipeline.Buffers.RelevantEntities[0].Length, Is.EqualTo(256));
        }

        [Test]
        public void ShrinkDenseArrays_WithPipelineBuffers_CallsResize()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 8192, 512);
            _pipeline = new ReplicationPipeline(4, _sut);

            // Act - register 129 entities to grow capacity to 256
            for (var i = 0; i < 129; i++)
                _sut.RegisterEntity(i, ElympicsPlayer.All);

            Assert.That(_pipeline.Buffers.RelevantEntities[0].Length, Is.EqualTo(256)); // Grown

            // Unregister entities down to the 3/8 threshold (96) to trigger shrink
            for (var i = 128; i >= 96; i--)
                _sut.UnregisterEntity(i);

            // Assert - both world and PipelineBuffers should shrink to 128
            Assert.That(_sut.DenseCapacity, Is.EqualTo(128));
            Assert.That(_pipeline.Buffers.RelevantEntities[0].Length, Is.EqualTo(128));
        }

        // =====================================================================
        // Category 5: IsValid
        // =====================================================================

        [Test]
        public void IsValid_RegisteredEntity_ReturnsTrue()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.RegisterEntity(5, ElympicsPlayer.All);

            // Act & Assert
            Assert.That(_sut.IsValid(5), Is.True);
        }

        [Test]
        public void IsValid_AfterUnregister_ReturnsFalse()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.RegisterEntity(5, ElympicsPlayer.All);
            _sut.UnregisterEntity(5);

            // Act & Assert
            Assert.That(_sut.IsValid(5), Is.False);
        }

        [Test]
        public void IsValid_StaleGeneration_ReturnsFalse_NewGeneration_ReturnsTrue()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            var gen1Id = NetworkIdConstants.EncodeNetworkId(1, 42);
            _sut.RegisterEntity(gen1Id, ElympicsPlayer.All);

            // Act - unregister and re-register with new generation
            _sut.UnregisterEntity(gen1Id);
            var gen2Id = NetworkIdConstants.EncodeNetworkId(2, 42);
            _sut.RegisterEntity(gen2Id, ElympicsPlayer.All);

            // Assert
            Assert.That(_sut.IsValid(gen1Id), Is.False);
            Assert.That(_sut.IsValid(gen2Id), Is.True);
        }

        [Test]
        public void IsValid_NeverRegistered_ReturnsFalse()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act & Assert
            Assert.That(_sut.IsValid(100), Is.False);
        }

        // =====================================================================
        // Category 6: GetDenseIndex/GetNetworkId
        // =====================================================================

        [Test]
        public void GetDenseIndex_RegisteredEntity_ReturnsCorrectIndex()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.RegisterEntity(10, ElympicsPlayer.All);
            _sut.RegisterEntity(20, ElympicsPlayer.All);

            // Act & Assert
            Assert.That(_sut.GetDenseIndex(20), Is.EqualTo(1));
        }

        [Test]
        public void GetDenseIndex_UnregisteredEntity_ReturnsMinusOne()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act & Assert
            Assert.That(_sut.GetDenseIndex(99), Is.EqualTo(-1));
        }

        [Test]
        public void GetNetworkId_ValidDenseIndex_ReturnsNetworkId()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.RegisterEntity(10, ElympicsPlayer.All);
            _sut.RegisterEntity(20, ElympicsPlayer.All);

            // Act & Assert
            Assert.That(_sut.GetNetworkId(1), Is.EqualTo(20));
        }

        // =====================================================================
        // Category 7: Static Helpers
        // =====================================================================

        [Test]
        public void ExtractIndex_ExtractsLow16Bits()
        {
            // Arrange
            var networkId = (5 << 16) | 42; // generation 5, index 42

            // Act
            var index = ElympicsWorld.ExtractIndex(networkId);

            // Assert
            Assert.That(index, Is.EqualTo(42));
        }

        [Test]
        public void ExtractGeneration_ExtractsBits16To31()
        {
            // Arrange
            var networkId = (5 << 16) | 42; // generation 5, index 42

            // Act
            var generation = ElympicsWorld.ExtractGeneration(networkId);

            // Assert
            Assert.That(generation, Is.EqualTo(5));
        }

        // =====================================================================
        // Category 8: BeginTick
        // =====================================================================

        [Test]
        public void BeginTick_TwoCallsRotateSnapshots()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            var snapshot1 = CreateSnapshot(100, 0);
            var snapshot2 = CreateSnapshot(101, 0);

            // Act
            _sut.BeginTick(snapshot1, 100);
            var firstCurrent = _sut.CurrentSnapshot;
            var firstPrevious = _sut.PreviousSnapshot;

            _sut.BeginTick(snapshot2, 101);
            var secondCurrent = _sut.CurrentSnapshot;
            var secondPrevious = _sut.PreviousSnapshot;

            // Assert
            Assert.That(firstCurrent, Is.EqualTo(snapshot1));
            Assert.That(firstPrevious.Tick, Is.EqualTo(-1));
            Assert.That(firstPrevious.Data, Is.Empty);
            Assert.That(secondCurrent, Is.EqualTo(snapshot2));
            Assert.That(secondPrevious, Is.EqualTo(snapshot1));
        }

        [Test]
        public void BeginTick_FirstCall_PreviousSnapshotIsEmpty()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            var snapshot = CreateSnapshot(100, 0);

            // Act
            _sut.BeginTick(snapshot, 100);

            // Assert
            Assert.That(_sut.CurrentSnapshot, Is.EqualTo(snapshot));
            Assert.That(_sut.PreviousSnapshot, Is.Not.Null);
            Assert.That(_sut.PreviousSnapshot.Tick, Is.EqualTo(-1));
            Assert.That(_sut.PreviousSnapshot.Data, Is.Empty);
        }

        // =====================================================================
        // Category 9: Shutdown
        // =====================================================================

        [Test]
        public void Shutdown_NullsEverything()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.RegisterEntity(5, ElympicsPlayer.All);

            // Act
            _sut.Dispose();

            // Assert - counters reset
            Assert.That(_sut.DenseCount, Is.EqualTo(0));
            Assert.That(_sut.DenseCapacity, Is.EqualTo(0));
        }

        // =====================================================================
        // Category 10: Lifecycle
        // =====================================================================

        [Test]
        public void Lifecycle_RegisterUnregisterReRegister_DifferentGenerations()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            var gen1Id = NetworkIdConstants.EncodeNetworkId(1, 42);
            var gen2Id = NetworkIdConstants.EncodeNetworkId(2, 42);

            // Act
            _sut.RegisterEntity(gen1Id, ElympicsPlayer.All);
            Assert.That(_sut.IsValid(gen1Id), Is.True);

            _sut.UnregisterEntity(gen1Id);
            Assert.That(_sut.IsValid(gen1Id), Is.False);

            _sut.RegisterEntity(gen2Id, ElympicsPlayer.All);
            Assert.That(_sut.IsValid(gen2Id), Is.True);

            // Assert
            Assert.That(_sut.IsValid(gen1Id), Is.False);
            Assert.That(_sut.IsValid(gen2Id), Is.True);
        }

        // =====================================================================
        // Category 11: Out-of-Bounds Sparse Index
        // =====================================================================

        [Test]
        public void RegisterEntity_SparseIndexExceedsCapacity_LogsError()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act & Assert
            LogAssert.Expect(LogType.Error, new Regex("sparse index.*exceeds capacity"));
            _sut.RegisterEntity(999, ElympicsPlayer.All);

            // Dense count should remain 0
            Assert.That(_sut.DenseCount, Is.EqualTo(0));
        }

        [Test]
        public void UnregisterEntity_SparseIndexExceedsCapacity_LogsError()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act & Assert
            LogAssert.Expect(LogType.Error, new Regex("sparse index.*exceeds capacity"));
            _sut.UnregisterEntity(999);

            // Dense count should remain 0
            Assert.That(_sut.DenseCount, Is.EqualTo(0));
        }

        [Test]
        public void RegisterEntity_ExactlyAtBoundary_LogsError()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act & Assert - 256 == array.Length, so it's out of bounds (off-by-one test)
            LogAssert.Expect(LogType.Error, new Regex("sparse index.*exceeds capacity"));
            _sut.RegisterEntity(256, ElympicsPlayer.All);

            // Dense count should remain 0
            Assert.That(_sut.DenseCount, Is.EqualTo(0));
        }

        [Test]
        public void RegisterEntity_MaxSparseSlotMinusOne_Succeeds()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);

            // Act - 255 is the last valid index (< 256)
            _sut.RegisterEntity(255, ElympicsPlayer.All);

            // Assert
            Assert.That(_sut.DenseCount, Is.EqualTo(1));
            Assert.That(_sut.SparseToDense[255], Is.EqualTo(0));
            Assert.That(_sut.DenseToSparse[0], Is.EqualTo(255));
        }
    }
}
