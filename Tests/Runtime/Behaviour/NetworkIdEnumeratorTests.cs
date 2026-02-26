using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Elympics.Tests.Runtime.Behaviour
{
    [TestFixture]
    [Category("NetworkIdEnumerator")]
    public class NetworkIdEnumeratorTests
    {
        private NetworkIdEnumerator _sut;

        [SetUp]
        public void SetUp() => _sut = null;

        [TearDown]
        public void TearDown() => _sut = null;

        #region Helper Methods

        /// <summary>
        /// Encodes a generation and index into a NetworkId using the same formula as production code.
        /// </summary>
        private static int Encode(int generation, int index) =>
            NetworkIdConstants.EncodeNetworkId(generation, index);

        /// <summary>
        /// Extracts the index component from a NetworkId.
        /// </summary>
        private static int ExtractIndex(int networkId) =>
            NetworkIdConstants.ExtractIndex(networkId);

        /// <summary>
        /// Extracts the generation component from a NetworkId.
        /// </summary>
        private static int ExtractGeneration(int networkId) =>
            NetworkIdConstants.ExtractGeneration(networkId);

        #endregion

        // =====================================================================
        // Category 1: Construction and Initial State
        // =====================================================================

        [UnityTest]
        public IEnumerator CreateWithRange_AfterConstruction_GetCurrentReturnsFirstAllocatedId()
        {
            // Arrange & Act
            _sut = NetworkIdEnumerator.CreateWithRange(100, 200);

            // Assert — constructor calls MoveNextAndGetCurrent() once,
            // so the first ID should be at index=min with generation=1
            var current = _sut.GetCurrent();
            var expectedId = Encode(1, 100);

            Assert.That(current,
                Is.EqualTo(expectedId),
                "Constructor pre-allocates first ID at index=min with generation=1");
            Assert.That(ExtractIndex(current), Is.EqualTo(100));
            Assert.That(ExtractGeneration(current), Is.EqualTo(1));

            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateWithRange_MaxClampedToMaxIndex_WhenMaxExceedsLimit()
        {
            // Arrange — pass max well beyond the 16-bit limit
            var hugeMax = NetworkIdConstants.MaxIndex + 5000;

            // Act — should not throw, max gets clamped internally
            _sut = NetworkIdEnumerator.CreateWithRange(0, hugeMax);

            // Assert — the first allocated ID should still be valid
            var current = _sut.GetCurrent();
            Assert.That(ExtractIndex(current), Is.EqualTo(0));
            Assert.That(ExtractGeneration(current), Is.EqualTo(1));

            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateWithRange_SingleSlotRange_AllocatesOneId()
        {
            // Arrange — min == max, range has exactly 1 index
            _sut = NetworkIdEnumerator.CreateWithRange(50, 50);

            // Assert — constructor pre-allocated the single available index
            var current = _sut.GetCurrent();
            Assert.That(ExtractIndex(current), Is.EqualTo(50));
            Assert.That(ExtractGeneration(current), Is.EqualTo(1));

            // Act — attempting a second allocation should overflow
            // ElympicsLogger.LogException(new OverflowException(...)) calls Debug.LogException before throwing
            LogAssert.Expect(LogType.Exception, new Regex("OverflowException: Cannot generate a network ID. The pool of indices between min: 50 and max: 50 has been used up."));

            _ = Assert.Throws<OverflowException>(() => _sut.MoveNextAndGetCurrent(),
                "Second allocation on single-slot range must throw OverflowException");

            yield return null;
        }

        // =====================================================================
        // Category 2: Sequential Allocation (Happy Path)
        // =====================================================================

        [UnityTest]
        public IEnumerator MoveNextAndGetCurrent_SequentialCalls_AllocatesConsecutiveIndicesWithGeneration1()
        {
            // Arrange
            const int min = 10;
            const int max = 19;
            _sut = NetworkIdEnumerator.CreateWithRange(min, max);

            // Act — constructor already allocated index 10; allocate 9 more
            var allocatedIds = new List<int> { _sut.GetCurrent() };
            for (var i = 1; i < 10; i++)
                allocatedIds.Add(_sut.MoveNextAndGetCurrent());

            // Assert — all 10 IDs should be consecutive indices from 10 to 19, all generation 1
            for (var i = 0; i < allocatedIds.Count; i++)
            {
                Assert.That(ExtractIndex(allocatedIds[i]),
                    Is.EqualTo(min + i),
                    $"ID at position {i} should have index {min + i}");
                Assert.That(ExtractGeneration(allocatedIds[i]),
                    Is.EqualTo(1),
                    $"All fresh IDs should have generation 1");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MoveNextAndGetCurrent_AllIdsHaveCorrectEncoding()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(5, 7);

            // Act
            var id0 = _sut.GetCurrent(); // index 5, gen 1
            var id1 = _sut.MoveNextAndGetCurrent(); // index 6, gen 1
            var id2 = _sut.MoveNextAndGetCurrent(); // index 7, gen 1

            // Assert — verify encode/decode round-trip
            Assert.That(id0, Is.EqualTo(Encode(1, 5)));
            Assert.That(id1, Is.EqualTo(Encode(1, 6)));
            Assert.That(id2, Is.EqualTo(Encode(1, 7)));

            Assert.That(ExtractIndex(id0), Is.EqualTo(5));
            Assert.That(ExtractIndex(id1), Is.EqualTo(6));
            Assert.That(ExtractIndex(id2), Is.EqualTo(7));

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetCurrent_ReturnsSameValueWithoutAdvancing()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 100);
            var firstCall = _sut.GetCurrent();

            // Act
            var secondCall = _sut.GetCurrent();
            var thirdCall = _sut.GetCurrent();

            // Assert
            Assert.That(secondCall, Is.EqualTo(firstCall));
            Assert.That(thirdCall, Is.EqualTo(firstCall));

            yield return null;
        }

        // =====================================================================
        // Category 3: Release and Reuse (FIFO Recycling)
        // =====================================================================

        [UnityTest]
        public IEnumerator ReleaseId_ThenMoveNext_ReusesReleasedIndexWithIncrementedGeneration()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(10, 20);
            var firstId = _sut.GetCurrent(); // index 10, gen 1

            // Act — release the first ID, then allocate again
            _sut.ReleaseId(firstId);
            var reusedId = _sut.MoveNextAndGetCurrent();

            // Assert — same index, generation bumped to 2
            Assert.That(ExtractIndex(reusedId),
                Is.EqualTo(10),
                "Released index should be reused from free queue");
            Assert.That(ExtractGeneration(reusedId),
                Is.EqualTo(2),
                "Generation should increment to 2 on reuse");
            Assert.That(reusedId, Is.EqualTo(Encode(2, 10)));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ReleaseId_MultipleReleases_ReusedInFIFOOrder()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 10);
            var idA = _sut.GetCurrent(); // index 0, gen 1
            var idB = _sut.MoveNextAndGetCurrent(); // index 1, gen 1
            var idC = _sut.MoveNextAndGetCurrent(); // index 2, gen 1

            // Act — release in order A, B, C
            _sut.ReleaseId(idA);
            _sut.ReleaseId(idB);
            _sut.ReleaseId(idC);

            // Next three allocations should reuse in FIFO order: A, B, C
            var reusedFirst = _sut.MoveNextAndGetCurrent();
            var reusedSecond = _sut.MoveNextAndGetCurrent();
            var reusedThird = _sut.MoveNextAndGetCurrent();

            // Assert
            Assert.That(ExtractIndex(reusedFirst),
                Is.EqualTo(0),
                "First reuse should be index 0 (A was released first)");
            Assert.That(ExtractIndex(reusedSecond),
                Is.EqualTo(1),
                "Second reuse should be index 1 (B was released second)");
            Assert.That(ExtractIndex(reusedThird),
                Is.EqualTo(2),
                "Third reuse should be index 2 (C was released third)");

            // All reused IDs should have generation 2
            Assert.That(ExtractGeneration(reusedFirst), Is.EqualTo(2));
            Assert.That(ExtractGeneration(reusedSecond), Is.EqualTo(2));
            Assert.That(ExtractGeneration(reusedThird), Is.EqualTo(2));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ReleaseId_ReusedIndexHasGenerationIncrementedByOne()
        {
            // Arrange — single-slot range to force repeated reuse of same index
            _sut = NetworkIdEnumerator.CreateWithRange(42, 42);

            // Act — cycle through 5 generations
            for (var expectedGen = 2; expectedGen <= 5; expectedGen++)
            {
                var currentId = _sut.GetCurrent();
                _sut.ReleaseId(currentId);
                var newId = _sut.MoveNextAndGetCurrent();

                // Assert — each reuse increments generation by exactly 1
                Assert.That(ExtractGeneration(newId),
                    Is.EqualTo(expectedGen),
                    $"After {expectedGen - 1} reuses, generation should be {expectedGen}");
                Assert.That(ExtractIndex(newId), Is.EqualTo(42));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ReleaseId_MixOfFreshAndReused_PrefersReusedFromFreeQueue()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 100);
            var firstId = _sut.GetCurrent(); // index 0, gen 1
            _ = _sut.MoveNextAndGetCurrent(); // index 1, gen 1
            _ = _sut.MoveNextAndGetCurrent(); // index 2, gen 1

            // Act — release the first ID, then allocate
            _sut.ReleaseId(firstId);
            var nextId = _sut.MoveNextAndGetCurrent();

            // Assert — should reuse released index 0 (from free queue), not fresh index 3
            Assert.That(ExtractIndex(nextId),
                Is.EqualTo(0),
                "Free queue has priority over fresh index allocation");
            Assert.That(ExtractGeneration(nextId),
                Is.EqualTo(2),
                "Reused index should have incremented generation");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ReleaseId_SceneObjectId_Generation0_DoesNotEnqueueForReuse()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 10);
            _ = _sut.GetCurrent(); // index 0, gen 1 (pre-allocated by constructor)

            // Fabricate a scene-object ID (generation 0) for index 5
            var sceneObjectId = Encode(0, 5);

            // Act — release a gen-0 ID; this should be silently ignored
            _sut.ReleaseId(sceneObjectId);

            // Allocate next — should get fresh index 1, NOT reused index 5
            var nextId = _sut.MoveNextAndGetCurrent();

            // Assert
            Assert.That(ExtractIndex(nextId),
                Is.EqualTo(1),
                "Scene object IDs (gen 0) must never be recycled");
            Assert.That(ExtractGeneration(nextId), Is.EqualTo(1));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ReleaseId_StaleId_DoesNotEnqueueForReuse()
        {
            // Arrange — single-slot range
            _sut = NetworkIdEnumerator.CreateWithRange(30, 30);
            var gen1Id = _sut.GetCurrent(); // index 30, gen 1

            // Release and re-allocate to bump generation
            _sut.ReleaseId(gen1Id);
            var gen2Id = _sut.MoveNextAndGetCurrent(); // index 30, gen 2
            Assert.That(ExtractGeneration(gen2Id), Is.EqualTo(2));

            // Act — release the STALE gen-1 ID; this should be detected as stale and ignored
            // ElympicsLogger.LogWarning is called but does not fail tests
            _sut.ReleaseId(gen1Id);

            // Release the current gen-2 ID and allocate again
            _sut.ReleaseId(gen2Id);
            var gen3Id = _sut.MoveNextAndGetCurrent();

            // Assert — generation should be 3, not corrupted by the stale release
            Assert.That(ExtractGeneration(gen3Id),
                Is.EqualTo(3),
                "Stale release must not corrupt the free queue or generation tracking");
            Assert.That(ExtractIndex(gen3Id), Is.EqualTo(30));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ReleaseId_InvalidIndex_BeyondGenerationsArray_DoesNotCrash()
        {
            // Arrange — small range, so the _generations array is small
            _sut = NetworkIdEnumerator.CreateWithRange(0, 5);

            // Fabricate an ID with an index far beyond what the enumerator tracks
            // and a non-zero generation so it doesn't hit the gen-0 early return
            var farAwayId = Encode(1, 60000);

            // Act & Assert — should not throw; just logs a warning
            Assert.DoesNotThrow(() => _sut.ReleaseId(farAwayId),
                "Releasing an ID with index beyond _generations.Length must not throw");

            yield return null;
        }

        // =====================================================================
        // Category 4: IsValid (Staleness Detection)
        // =====================================================================

        [UnityTest]
        public IEnumerator IsValid_ActiveDynamicId_ReturnsTrue()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 10);
            var activeId = _sut.GetCurrent();

            // Act & Assert
            Assert.That(_sut.IsValid(activeId),
                Is.True,
                "Currently allocated dynamic ID should be valid");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsValid_AfterReleaseAndReallocation_StaleIdReturnsFalse()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 10);
            var gen1Id = _sut.GetCurrent(); // index 0, gen 1

            // Act — release and re-allocate (gen 2)
            _sut.ReleaseId(gen1Id);
            _ = _sut.MoveNextAndGetCurrent(); // index 0, gen 2

            // Assert
            Assert.That(_sut.IsValid(gen1Id),
                Is.False,
                "Stale ID (old generation) should be invalid");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsValid_AfterReleaseAndReallocation_NewIdReturnsTrue()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 10);
            var gen1Id = _sut.GetCurrent();

            // Act
            _sut.ReleaseId(gen1Id);
            var gen2Id = _sut.MoveNextAndGetCurrent();

            // Assert
            Assert.That(_sut.IsValid(gen2Id),
                Is.True,
                "Newly allocated ID with current generation should be valid");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsValid_Generation0_AlwaysReturnsTrue()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 10);

            // Act & Assert — gen-0 IDs are scene objects, always valid
            Assert.That(_sut.IsValid(Encode(0, 0)), Is.True);
            Assert.That(_sut.IsValid(Encode(0, 999)), Is.True);
            Assert.That(_sut.IsValid(Encode(0, 0xFFFF)), Is.True);

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsValid_IndexBeyondGenerationsArray_ReturnsFalse()
        {
            // Arrange — small range, _generations array is small
            _sut = NetworkIdEnumerator.CreateWithRange(0, 5);

            // Fabricate an ID with generation > 0 but index far beyond tracked range
            var fabricatedId = Encode(1, 50000);

            // Act & Assert
            Assert.That(_sut.IsValid(fabricatedId),
                Is.False,
                "ID with index beyond _generations array must be invalid");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsValid_NeverAllocatedIndex_ReturnsFalse()
        {
            // Arrange — allocate only index 0 (via constructor)
            _sut = NetworkIdEnumerator.CreateWithRange(0, 100);

            // Fabricate an ID at index 50 with gen 1 — never actually allocated
            var fabricatedId = Encode(1, 50);

            // Act & Assert — _generations[50] is 0, so gen 1 does not match
            Assert.That(_sut.IsValid(fabricatedId),
                Is.False,
                "An ID at an index that was never allocated should be invalid");

            yield return null;
        }

        // =====================================================================
        // Category 5: MoveTo
        // =====================================================================

        [UnityTest]
        public IEnumerator MoveTo_SetsCurrent_GetCurrentReturnsNewValue()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 100);
            var arbitraryValue = 999999;

            // Act
            _sut.MoveTo(arbitraryValue);

            // Assert
            Assert.That(_sut.GetCurrent(),
                Is.EqualTo(arbitraryValue),
                "MoveTo should set _current to the exact value provided");

            yield return null;
        }

        [UnityTest]
        public IEnumerator MoveTo_DoesNotAffectNextAllocation()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 100);
            _ = _sut.GetCurrent(); // index 0 pre-allocated

            // Act — move cursor to an arbitrary value
            _sut.MoveTo(12345);

            // Then allocate next — should use internal allocation logic, not the MoveTo value
            var nextId = _sut.MoveNextAndGetCurrent();

            // Assert — should allocate fresh index 1 (next after constructor's index 0)
            Assert.That(ExtractIndex(nextId),
                Is.EqualTo(1),
                "MoveTo only changes _current, not the allocation sequence");
            Assert.That(ExtractGeneration(nextId), Is.EqualTo(1));

            yield return null;
        }

        // =====================================================================
        // Category 6: Index Pool Exhaustion (Overflow)
        // =====================================================================

        [UnityTest]
        public IEnumerator MoveNextAndGetCurrent_AllIndicesExhausted_ThrowsOverflowException()
        {
            // Arrange — range of exactly 5 indices [10, 14]
            _sut = NetworkIdEnumerator.CreateWithRange(10, 14);

            // Constructor pre-allocated 1; allocate remaining 4
            for (var i = 0; i < 4; i++)
                _ = _sut.MoveNextAndGetCurrent();

            // Act & Assert — 6th allocation should overflow
            LogAssert.Expect(LogType.Exception, new Regex("OverflowException: Cannot generate a network ID. The pool of indices between min: 10 and max: 14 has been used up."));
            _ = Assert.Throws<OverflowException>(() => _sut.MoveNextAndGetCurrent(),
                "Allocating beyond the index pool must throw OverflowException");

            yield return null;
        }

        [UnityTest]
        public IEnumerator MoveNextAndGetCurrent_AllIndicesExhausted_ThenReleaseOne_CanAllocateAgain()
        {
            // Arrange — range of 3 indices [0, 2]
            _sut = NetworkIdEnumerator.CreateWithRange(0, 2);
            _ = _sut.GetCurrent(); // index 0
            var id1 = _sut.MoveNextAndGetCurrent(); // index 1
            _ = _sut.MoveNextAndGetCurrent(); // index 2
            // All 3 indices exhausted

            // Act — release one, then allocate
            _sut.ReleaseId(id1);
            var recycledId = _sut.MoveNextAndGetCurrent();

            // Assert — should reuse index 1 with incremented generation
            Assert.That(ExtractIndex(recycledId), Is.EqualTo(1));
            Assert.That(ExtractGeneration(recycledId),
                Is.EqualTo(2),
                "Recycled index should have generation 2");

            yield return null;
        }

        [UnityTest]
        public IEnumerator MoveNextAndGetCurrent_SmallRange_ExactBoundaryCount()
        {
            // Arrange — range of exactly 3 indices [20, 22]
            _sut = NetworkIdEnumerator.CreateWithRange(20, 22);

            // Act — constructor pre-allocated 1; allocate 2 more
            var id1 = _sut.GetCurrent(); // index 20
            var id2 = _sut.MoveNextAndGetCurrent(); // index 21
            var id3 = _sut.MoveNextAndGetCurrent(); // index 22

            // Assert — all 3 allocated successfully
            Assert.That(ExtractIndex(id1), Is.EqualTo(20));
            Assert.That(ExtractIndex(id2), Is.EqualTo(21));
            Assert.That(ExtractIndex(id3), Is.EqualTo(22));

            // 4th should throw
            LogAssert.Expect(LogType.Exception, new Regex("OverflowException: Cannot generate a network ID. The pool of indices between min: 20 and max: 22 has been used up."));
            _ = Assert.Throws<OverflowException>(() => _sut.MoveNextAndGetCurrent());

            yield return null;
        }

        // =====================================================================
        // Category 7: Generation Overflow
        // =====================================================================

        [UnityTest]
        public IEnumerator MoveNextAndGetCurrent_GenerationOverflow_ThrowsOverflowException()
        {
            // Arrange — single-slot range to force rapid generation cycling
            _sut = NetworkIdEnumerator.CreateWithRange(0, 0);

            // Constructor pre-allocates gen 1. Cycle through all generations up to ushort.MaxValue.
            // Each cycle: release current, allocate next (gen 2, 3, ..., 65535).
            // Total cycles needed: 65534 (from gen 1 to gen 65535).
            for (var gen = 2; gen <= ushort.MaxValue; gen++)
            {
                _sut.ReleaseId(_sut.GetCurrent());
                _ = _sut.MoveNextAndGetCurrent();
            }

            // Current generation is now ushort.MaxValue (65535)
            Assert.That(ExtractGeneration(_sut.GetCurrent()), Is.EqualTo(ushort.MaxValue));

            // Act — one more release + allocate should overflow the generation counter
            _sut.ReleaseId(_sut.GetCurrent());
            LogAssert.Expect(LogType.Exception, new Regex(@"OverflowException: Cannot use new networkId generation\. The pool of generations \(min: 0, max: 65535\) has been used up\."));
            _ = Assert.Throws<OverflowException>(() => _sut.MoveNextAndGetCurrent(),
                "Generation overflow must throw OverflowException");

            yield return null;
        }

        // =====================================================================
        // Category 8: Capacity Growth (EnsureGenerationCapacity)
        // =====================================================================

        [UnityTest]
        public IEnumerator MoveNextAndGetCurrent_IndexBeyondInitialCapacity_GrowsGenerationsArray()
        {
            // Arrange — start beyond the initial _generations array capacity of 256
            const int startIndex = 300;
            _sut = NetworkIdEnumerator.CreateWithRange(startIndex, startIndex + 10);

            // Act — constructor already allocated index 300 (beyond initial capacity 256)
            var firstId = _sut.GetCurrent();

            // Assert — allocation succeeds and ID is valid
            Assert.That(ExtractIndex(firstId), Is.EqualTo(startIndex));
            Assert.That(ExtractGeneration(firstId), Is.EqualTo(1));
            Assert.That(_sut.IsValid(firstId),
                Is.True,
                "ID at index beyond initial capacity should be valid after array growth");

            yield return null;
        }

        [UnityTest]
        public IEnumerator EnsureGenerationCapacity_PreservesExistingGenerations_AfterGrowth()
        {
            // Arrange — start within initial capacity (256), then grow beyond it
            _sut = NetworkIdEnumerator.CreateWithRange(0, 500);

            // Allocate a few IDs within initial capacity
            var id0 = _sut.GetCurrent(); // index 0
            var id1 = _sut.MoveNextAndGetCurrent(); // index 1

            // Act — allocate enough IDs to force array growth beyond 256
            var lastId = id1;
            for (var i = 2; i <= 260; i++)
                lastId = _sut.MoveNextAndGetCurrent();

            // Assert — earlier IDs should still be valid (generations preserved through growth)
            Assert.That(_sut.IsValid(id0),
                Is.True,
                "ID allocated before capacity growth must remain valid");
            Assert.That(_sut.IsValid(id1),
                Is.True,
                "ID allocated before capacity growth must remain valid");
            Assert.That(_sut.IsValid(lastId),
                Is.True,
                "ID allocated after capacity growth must be valid");
            Assert.That(ExtractIndex(lastId), Is.EqualTo(260));

            yield return null;
        }

        // =====================================================================
        // Category 9: CreateForPlayer Factory Method
        // =====================================================================

        [UnityTest]
        public IEnumerator CreateForPlayer_RegularPlayer_AllocatesInCorrectRange()
        {
            // Arrange
            const int playerIndex = 0;
            const int indicesPerPlayer = 100;
            const int sceneObjectsMaxIndex = 1999;
            var spawnableSpecialPlayerIndices = new[] { -3, -2 }; // All, World

            // Act
            _sut = NetworkIdEnumerator.CreateForPlayer(
                playerIndex,
                indicesPerPlayer,
                sceneObjectsMaxIndex,
                spawnableSpecialPlayerIndices);

            // Assert — player 0 is slot 2 (after 2 special players), so start = 2000 + 2*100 = 2200
            var expectedStart = sceneObjectsMaxIndex + 1 + (playerIndex + spawnableSpecialPlayerIndices.Length) * indicesPerPlayer;
            var firstId = _sut.GetCurrent();
            Assert.That(ExtractIndex(firstId),
                Is.EqualTo(expectedStart),
                "Regular player 0 should start at the correct offset");
            Assert.That(ExtractGeneration(firstId), Is.EqualTo(1));

            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateForPlayer_SpecialPlayer_AllocatesInCorrectRange()
        {
            // Arrange — special player "All" has index -3
            const int playerIndex = -3;
            const int indicesPerPlayer = 100;
            const int sceneObjectsMaxIndex = 1999;
            var spawnableSpecialPlayerIndices = new[] { -3, -2 };

            // Act
            _sut = NetworkIdEnumerator.CreateForPlayer(
                playerIndex,
                indicesPerPlayer,
                sceneObjectsMaxIndex,
                spawnableSpecialPlayerIndices);

            // Assert — "All" is at slot 0, so start = 2000
            var expectedStart = sceneObjectsMaxIndex + 1;
            var firstId = _sut.GetCurrent();
            Assert.That(ExtractIndex(firstId),
                Is.EqualTo(expectedStart),
                "Special player 'All' (slot 0) should start right after scene objects");

            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateForPlayer_DifferentPlayers_HaveNonOverlappingRanges()
        {
            // Arrange
            const int indicesPerPlayer = 50;
            const int sceneObjectsMaxIndex = 999;
            var spawnableSpecialPlayerIndices = new[] { -3, -2 };

            var enumeratorAll = NetworkIdEnumerator.CreateForPlayer(
                -3,
                indicesPerPlayer,
                sceneObjectsMaxIndex,
                spawnableSpecialPlayerIndices);
            var enumeratorWorld = NetworkIdEnumerator.CreateForPlayer(
                -2,
                indicesPerPlayer,
                sceneObjectsMaxIndex,
                spawnableSpecialPlayerIndices);
            var enumeratorPlayer0 = NetworkIdEnumerator.CreateForPlayer(
                0,
                indicesPerPlayer,
                sceneObjectsMaxIndex,
                spawnableSpecialPlayerIndices);

            // Act — exhaust all 50 indices from each enumerator
            var indicesAll = new HashSet<int>();
            var indicesWorld = new HashSet<int>();
            var indicesPlayer0 = new HashSet<int>();

            _ = indicesAll.Add(ExtractIndex(enumeratorAll.GetCurrent()));
            _ = indicesWorld.Add(ExtractIndex(enumeratorWorld.GetCurrent()));
            _ = indicesPlayer0.Add(ExtractIndex(enumeratorPlayer0.GetCurrent()));

            for (var i = 1; i < indicesPerPlayer; i++)
            {
                _ = indicesAll.Add(ExtractIndex(enumeratorAll.MoveNextAndGetCurrent()));
                _ = indicesWorld.Add(ExtractIndex(enumeratorWorld.MoveNextAndGetCurrent()));
                _ = indicesPlayer0.Add(ExtractIndex(enumeratorPlayer0.MoveNextAndGetCurrent()));
            }

            // Assert — no index overlap between any two players
            Assert.That(indicesAll.Count, Is.EqualTo(indicesPerPlayer));
            Assert.That(indicesWorld.Count, Is.EqualTo(indicesPerPlayer));
            Assert.That(indicesPlayer0.Count, Is.EqualTo(indicesPerPlayer));

            Assert.That(indicesAll.Overlaps(indicesWorld),
                Is.False,
                "All and World ranges must not overlap");
            Assert.That(indicesAll.Overlaps(indicesPlayer0),
                Is.False,
                "All and Player0 ranges must not overlap");
            Assert.That(indicesWorld.Overlaps(indicesPlayer0),
                Is.False,
                "World and Player0 ranges must not overlap");

            yield return null;
        }

        // =====================================================================
        // Category 10: Fresh Index Wraparound Logic
        // =====================================================================

        [UnityTest]
        public IEnumerator GetNextFreshIndex_SkipsAlreadyUsedIndices()
        {
            // Arrange — range [0, 5], constructor allocates index 0 (gen 1)
            _sut = NetworkIdEnumerator.CreateWithRange(0, 5);
            var id0 = _sut.GetCurrent(); // index 0

            // Release index 0 (now gen[0] = 1, meaning "used")
            _sut.ReleaseId(id0);

            // Allocate next — free queue has index 0, so it will reuse it (gen 2)
            var reusedId = _sut.MoveNextAndGetCurrent(); // reuses index 0, gen 2
            Assert.That(ExtractIndex(reusedId), Is.EqualTo(0));

            // Allocate next — free queue is empty, fresh allocation starts at _nextFreshIndex (1)
            var freshId = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(freshId),
                Is.EqualTo(1),
                "Fresh allocation should skip used indices and find the next available");

            // Release the reused index 0 and allocate again
            _sut.ReleaseId(reusedId);

            var nextFromQueue = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(nextFromQueue),
                Is.EqualTo(0),
                "Free queue items are preferred over fresh indices");

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetNextFreshIndex_WrapsAroundToMin_WhenNextFreshExceedsMax()
        {
            // Arrange — range [5, 8], 4 indices total
            // Constructor allocates index 5 (gen 1), _nextFreshIndex becomes 6
            _sut = NetworkIdEnumerator.CreateWithRange(5, 8);

            // Allocate indices 6, 7, 8
            _ = _sut.MoveNextAndGetCurrent(); // index 6
            _ = _sut.MoveNextAndGetCurrent(); // index 8
            _ = _sut.MoveNextAndGetCurrent(); // index 7
            // _nextFreshIndex is now 9 (beyond max=8)

            // Release index 5 and 7 — they go into the free queue
            _sut.ReleaseId(Encode(1, 5));
            _sut.ReleaseId(Encode(1, 7));

            // Next two allocations should come from the free queue (FIFO: 5, then 7)
            var reused5 = _sut.MoveNextAndGetCurrent();
            var reused7 = _sut.MoveNextAndGetCurrent();

            Assert.That(ExtractIndex(reused5), Is.EqualTo(5), "FIFO: index 5 released first");
            Assert.That(ExtractIndex(reused7), Is.EqualTo(7), "FIFO: index 7 released second");

            Assert.That(ExtractGeneration(reused5), Is.EqualTo(2));
            Assert.That(ExtractGeneration(reused7), Is.EqualTo(2));

            yield return null;
        }

        // =====================================================================
        // Category 11: Edge Cases and Defensive Behavior
        // =====================================================================

        [UnityTest]
        public IEnumerator ReleaseId_RemovesFromDynamicAllocatedIds()
        {
            // Arrange — single-slot range
            _sut = NetworkIdEnumerator.CreateWithRange(77, 77);
            var gen1Id = _sut.GetCurrent(); // index 77, gen 1

            // Act — release, then re-allocate
            _sut.ReleaseId(gen1Id);
            var gen2Id = _sut.MoveNextAndGetCurrent(); // index 77, gen 2

            // Assert — the new ID is valid and was successfully allocated
            Assert.That(gen2Id,
                Is.Not.EqualTo(gen1Id),
                "Re-allocated ID should have different generation");
            Assert.That(ExtractIndex(gen2Id), Is.EqualTo(77));
            Assert.That(ExtractGeneration(gen2Id), Is.EqualTo(2));

            yield return null;
        }

        [UnityTest]
        public IEnumerator MoveNextAndGetCurrent_AfterFullCycleWithReleases_AllIdsUnique()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 9);
            var activeIds = new HashSet<int>
            {
                // Allocate 10 IDs
                _sut.GetCurrent()
            };

            for (var i = 1; i < 10; i++)
                _ = activeIds.Add(_sut.MoveNextAndGetCurrent());

            Assert.That(activeIds.Count, Is.EqualTo(10), "All 10 initial IDs should be unique");

            // Release 5 of them (indices 0, 2, 4, 6, 8)
            var toRelease = activeIds.Where(id => ExtractIndex(id) % 2 == 0).ToList();

            foreach (var id in toRelease)
            {
                _ = activeIds.Remove(id);
                _sut.ReleaseId(id);
            }

            Assert.That(activeIds.Count, Is.EqualTo(5));

            // Allocate 5 more (should reuse released indices with gen 2)
            for (var i = 0; i < 5; i++)
            {
                var newId = _sut.MoveNextAndGetCurrent();
                Assert.That(activeIds.Add(newId),
                    Is.True,
                    $"Newly allocated ID {newId} must be unique among all active IDs");
            }

            // Assert — all 10 active IDs are unique
            Assert.That(activeIds.Count, Is.EqualTo(10));

            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateWithRange_MinEqualsMax_AllocatesExactlyOneId()
        {
            // Arrange & Act
            _sut = NetworkIdEnumerator.CreateWithRange(999, 999);

            // Assert — exactly one ID allocated by constructor
            var onlyId = _sut.GetCurrent();
            Assert.That(ExtractIndex(onlyId), Is.EqualTo(999));
            Assert.That(ExtractGeneration(onlyId), Is.EqualTo(1));

            // Second allocation must fail
            LogAssert.Expect(LogType.Exception, new Regex("OverflowException: Cannot generate a network ID. The pool of indices between min: 999 and max: 999 has been used up."));
            _ = Assert.Throws<OverflowException>(() => _sut.MoveNextAndGetCurrent());

            yield return null;
        }

        // =====================================================================
        // Additional robustness tests
        // =====================================================================

        [UnityTest]
        public IEnumerator IsValid_MultipleIndices_TrackedIndependently()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 5);
            var id0 = _sut.GetCurrent(); // index 0, gen 1
            var id1 = _sut.MoveNextAndGetCurrent(); // index 1, gen 1
            var id2 = _sut.MoveNextAndGetCurrent(); // index 2, gen 1

            // Release only index 1 and re-allocate it (now gen 2)
            _sut.ReleaseId(id1);
            var id1Gen2 = _sut.MoveNextAndGetCurrent();

            // Assert — id0 and id2 still valid, old id1 is stale, new id1 is valid
            Assert.That(_sut.IsValid(id0), Is.True, "id0 should still be valid");
            Assert.That(_sut.IsValid(id1), Is.False, "Old gen-1 id1 should be stale");
            Assert.That(_sut.IsValid(id1Gen2), Is.True, "New gen-2 id1 should be valid");
            Assert.That(_sut.IsValid(id2), Is.True, "id2 should still be valid");

            yield return null;
        }

        [UnityTest]
        public IEnumerator MoveNextAndGetCurrent_ReturnsValueConsistentWithGetCurrent()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 100);

            // Act & Assert — MoveNextAndGetCurrent return value must equal subsequent GetCurrent
            for (var i = 0; i < 5; i++)
            {
                var moveNextResult = _sut.MoveNextAndGetCurrent();
                var getCurrent = _sut.GetCurrent();
                Assert.That(getCurrent,
                    Is.EqualTo(moveNextResult),
                    "GetCurrent must return the same value as the last MoveNextAndGetCurrent");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ReleaseId_AllReleasedThenReallocated_GenerationsAllIncremented()
        {
            // Arrange — range [0, 4], 5 indices
            _sut = NetworkIdEnumerator.CreateWithRange(0, 4);

            var firstRoundIds = new List<int> { _sut.GetCurrent() };
            for (var i = 1; i < 5; i++)
                firstRoundIds.Add(_sut.MoveNextAndGetCurrent());

            // All should be generation 1
            foreach (var id in firstRoundIds)
                Assert.That(ExtractGeneration(id), Is.EqualTo(1));

            // Release all in order
            foreach (var id in firstRoundIds)
                _sut.ReleaseId(id);

            // Re-allocate all 5
            var secondRoundIds = new List<int> { _sut.MoveNextAndGetCurrent() };
            for (var i = 1; i < 5; i++)
                secondRoundIds.Add(_sut.MoveNextAndGetCurrent());

            // Assert — all should be generation 2, reused in FIFO order
            for (var i = 0; i < 5; i++)
            {
                Assert.That(ExtractIndex(secondRoundIds[i]),
                    Is.EqualTo(i),
                    $"Second round ID {i} should reuse index {i} in FIFO order");
                Assert.That(ExtractGeneration(secondRoundIds[i]),
                    Is.EqualTo(2),
                    $"Second round ID {i} should have generation 2");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MoveTo_ThenGetCurrent_ReflectsNewValue_EvenAfterAllocation()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 100);
            _ = _sut.GetCurrent();
            _ = _sut.MoveNextAndGetCurrent();

            // Act — override cursor
            var overrideValue = Encode(5, 42);
            _sut.MoveTo(overrideValue);

            // Assert — GetCurrent reflects the override
            Assert.That(_sut.GetCurrent(), Is.EqualTo(overrideValue));

            // Next allocation still works independently
            var nextAllocated = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractGeneration(nextAllocated),
                Is.EqualTo(1),
                "Allocation after MoveTo should still use internal state");

            yield return null;
        }
    }
}
