using System;
using NUnit.Framework;

namespace Elympics.Tests
{
    /// <summary>
    /// Tests for the NetworkId enumerator synchronization fix.
    /// Covers <see cref="NetworkIdEnumerator.SyncAllocatedId"/> and the updated
    /// <see cref="DynamicElympicsBehaviourInstanceData"/> structure that stores explicit NetworkIds.
    /// </summary>
    [TestFixture]
    [Category("NetworkIdSync")]
    public class NetworkIdSyncTests
    {
        private NetworkIdEnumerator _sut;

        [TearDown]
        public void TearDown() => _sut = null;

        private static int Encode(int generation, int index) =>
            NetworkIdConstants.EncodeNetworkId(generation, index);

        private static int ExtractIndex(int networkId) =>
            NetworkIdConstants.ExtractIndex(networkId);

        private static int ExtractGeneration(int networkId) =>
            NetworkIdConstants.ExtractGeneration(networkId);

        // =====================================================================
        // SyncAllocatedId — basic registration
        // =====================================================================

        [Test]
        public void SyncAllocatedId_RegistersIdAsAllocated_IsValidReturnsTrue()
        {
            // Arrange — range [100, 200]; constructor already allocates index 100 gen 1
            _sut = NetworkIdEnumerator.CreateWithRange(100, 200);

            // Fabricate an id at index 150 gen 1 — as if the server allocated it
            var serverAssignedId = Encode(1, 150);

            // Act
            _sut.SyncAllocatedId(serverAssignedId);

            // Assert — the enumerator now considers this id valid
            Assert.That(_sut.IsValid(serverAssignedId),
                Is.True,
                "After SyncAllocatedId the id should be considered valid");
        }

        [Test]
        public void SyncAllocatedId_AdvancesNextFreshIndex_PastSyncedIndex()
        {
            // Arrange — range [10, 50]; constructor allocates index 10
            _sut = NetworkIdEnumerator.CreateWithRange(10, 50);

            // Sync index 20 as if the server used it
            _sut.SyncAllocatedId(Encode(1, 20));

            // Act — release index 10 so the free queue is used, then allocate until we would
            // normally reach index 20 from fresh allocation
            _sut.ReleaseId(_sut.GetCurrent()); // free index 10
            _ = _sut.MoveNextAndGetCurrent(); // reuses index 10 from free queue (gen 2)

            // Now fresh allocation starts at _nextFreshIndex which must be > 20
            var freshId = _sut.MoveNextAndGetCurrent();

            // Assert — fresh allocation must not land on index 20 (already synced)
            Assert.That(ExtractIndex(freshId),
                Is.Not.EqualTo(20),
                "Fresh allocation must skip any index synced via SyncAllocatedId");
        }

        [Test]
        public void SyncAllocatedId_MultipleIds_NoneAreReallocated()
        {
            // Arrange — range [0, 99]; constructor allocates index 0 gen 1
            _sut = NetworkIdEnumerator.CreateWithRange(0, 99);

            // Sync a batch of server-assigned ids
            var serverIds = new[] { Encode(1, 5), Encode(1, 15), Encode(1, 25) };
            foreach (var id in serverIds)
                _sut.SyncAllocatedId(id);

            // Allocate several more ids via the enumerator
            var allocatedIndexes = new System.Collections.Generic.HashSet<int>();
            _ = allocatedIndexes.Add(ExtractIndex(_sut.GetCurrent())); // index 0 (from ctor)

            for (var i = 0; i < 20; i++)
                _ = allocatedIndexes.Add(ExtractIndex(_sut.MoveNextAndGetCurrent()));

            // Assert — none of the synced indexes appear in the newly allocated set
            foreach (var serverId in serverIds)
            {
                Assert.That(allocatedIndexes.Contains(ExtractIndex(serverId)),
                    Is.False,
                    $"Index {ExtractIndex(serverId)} was synced by server and must not be reallocated");
            }
        }

        [Test]
        public void SyncAllocatedId_Generation0_IsIgnored()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 99);
            var sceneObjectId = Encode(0, 5);

            // Act — syncing a scene object id must be silently ignored
            Assert.DoesNotThrow(() => _sut.SyncAllocatedId(sceneObjectId),
                "SyncAllocatedId must not throw for generation-0 ids");

            // The enumerator should still allocate index 5 normally if needed
            _ = _sut.GetCurrent(); // index 0 from ctor
            _sut.ReleaseId(_sut.GetCurrent()); // free index 0
            _ = _sut.MoveNextAndGetCurrent(); // reuses 0 from free queue

            // The contract: generation-0 sync must not advance _nextFreshIndex past the synced
            // index (5). The exact value depends on internal allocation order, but it must stay
            // well below 5 — we assert < 5 to express the contract without over-specifying.
            var next = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(next),
                Is.LessThan(5),
                "Scene-object sync must not influence fresh index pointer");
        }

        [Test]
        public void SyncAllocatedId_IndexOutsideRange_IsIgnored()
        {
            // Arrange — range [100, 200]
            _sut = NetworkIdEnumerator.CreateWithRange(100, 200);

            // Id whose index is outside [100, 200]
            var outsideId = Encode(1, 50);

            // Act — must not throw
            Assert.DoesNotThrow(() => _sut.SyncAllocatedId(outsideId),
                "SyncAllocatedId must ignore ids outside the enumerator's range");

            // Fresh allocation should still start at index 101 (100 was allocated by ctor)
            var nextId = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(nextId),
                Is.EqualTo(101),
                "Out-of-range sync must not affect internal fresh index pointer");
        }

        [Test]
        public void SyncAllocatedId_BeyondInitialCapacity_TriggersEnsureGenerationCapacity()
        {
            // SyncAllocatedId with an index beyond InitialGenerationsCapacity (256) must
            // trigger EnsureGenerationCapacity and correctly register the id.
            var sut = NetworkIdEnumerator.CreateWithRange(300, 400);

            var syncedId = NetworkIdEnumerator.EncodeNetworkId(1, 350);
            sut.SyncAllocatedId(syncedId);

            // The synced id should be valid
            Assert.That(sut.IsValid(syncedId), Is.True, "Synced id beyond initial capacity must be valid");

            // Next allocation must not collide with the synced index
            var nextId = sut.MoveNextAndGetCurrent();
            Assert.That(NetworkIdEnumerator.ExtractIndex(nextId),
                Is.Not.EqualTo(350),
                "Fresh allocation must skip the synced index");
        }

        // =====================================================================
        // SyncAllocatedId — ReleaseId interop
        // =====================================================================

        [Test]
        public void SyncAllocatedId_ThenReleaseId_WorksCorrectly()
        {
            // Arrange
            _sut = NetworkIdEnumerator.CreateWithRange(0, 99);
            var serverAssignedId = Encode(1, 10);
            _sut.SyncAllocatedId(serverAssignedId);

            // Act — release the synced id (simulates client destroying an authoritative object)
            Assert.DoesNotThrow(() => _sut.ReleaseId(serverAssignedId),
                "ReleaseId should work for ids registered via SyncAllocatedId");

            // Index 10 is now in the free queue — next allocation reuses it directly (FIFO)
            var reusedId = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(reusedId),
                Is.EqualTo(10),
                "Index released via ReleaseId after SyncAllocatedId should be reusable");
            Assert.That(ExtractGeneration(reusedId),
                Is.EqualTo(2),
                "Reused index should have incremented generation");
        }

        [Test]
        public void SyncAllocatedId_ServerAuthoritative_GenerationAlwaysMatchesServer()
        {
            // The server is authoritative — SyncAllocatedId must unconditionally accept
            // the server's generation. The destroy-before-create ordering in OnPostStateApplied
            // guarantees that any conflicting entity has already been destroyed.
            _sut = NetworkIdEnumerator.CreateWithRange(0, 99);
            _ = _sut.GetCurrent(); // index 0
            var id5Gen1 = Encode(1, 5);
            _sut.SyncAllocatedId(id5Gen1);

            // Advance to gen 2 (simulating client prediction racing ahead)
            _sut.ReleaseId(id5Gen1);
            var id5Gen2 = Encode(2, 5);
            _sut.SyncAllocatedId(id5Gen2);

            // Server snapshot arrives with gen 1 (server never saw the predicted spawn/destroy).
            // During reconciliation, the gen-2 entity was already destroyed before this call.
            _sut.ReleaseId(id5Gen2);
            _sut.SyncAllocatedId(id5Gen1);

            // The server's generation wins — gen 1 is now the live id
            Assert.That(_sut.IsValid(id5Gen1),
                Is.True,
                "Server-authoritative sync must set generation to the server's value");
            Assert.That(_sut.IsValid(id5Gen2),
                Is.False,
                "The predicted generation must no longer be valid after server sync");

            // ReleaseId must work correctly for the server's generation
            Assert.DoesNotThrow(() => _sut.ReleaseId(id5Gen1),
                "ReleaseId must succeed for the server-authoritative generation");
        }

        [Test]
        public void GetNext_SyncBeforeRelease_IndexRemainsRecyclable()
        {
            // Scenario: Server allocates id, client syncs it, then releases it.
            // The index must still be recyclable after release.
            var sut = NetworkIdEnumerator.CreateWithRange(100, 200);

            // Constructor already allocated index 100 gen 1 — retrieve it
            var id1 = sut.GetCurrent();
            var index1 = NetworkIdEnumerator.ExtractIndex(id1);
            Assert.That(index1, Is.EqualTo(100));

            // Release it — index 100 enters the free queue
            sut.ReleaseId(id1);

            // SyncAllocatedId re-registers the same id (simulating server snapshot).
            // Index 100 is now live in _dynamicAllocatedIds; it is removed from _freeIndicesSet
            // so a subsequent ReleaseId is not treated as a double-release.
            sut.SyncAllocatedId(id1);

            // Release again (server destroyed the object) — index 100 re-enters free queue
            sut.ReleaseId(id1);

            // id2 reuses index 100 with generation 2 from the free queue
            var id2 = sut.MoveNextAndGetCurrent();

            // id3 comes from fresh allocation at index 101
            var id3 = sut.MoveNextAndGetCurrent();

            Assert.That(NetworkIdEnumerator.ExtractIndex(id2), Is.EqualTo(100), "id2 must reuse index 100 from free queue");
            Assert.That(NetworkIdEnumerator.ExtractIndex(id3), Is.EqualTo(101), "id3 must be fresh index 101");
        }

        [Test]
        public void GetNext_ReleaseIdSyncAllocatedIdReleaseId_DoubleQueueEntryHandledCorrectly()
        {
            // Scenario: ReleaseId → SyncAllocatedId → ReleaseId on the same index creates
            // two physical entries in _freeIndices. Both must be handled without error or
            // duplicate allocation.
            var sut = NetworkIdEnumerator.CreateWithRange(10, 20);

            // Constructor already allocated index 10 gen 1 — retrieve it
            var id1 = sut.GetCurrent();
            Assert.That(NetworkIdEnumerator.ExtractIndex(id1), Is.EqualTo(10));
            Assert.That(NetworkIdEnumerator.ExtractGeneration(id1), Is.EqualTo(1));

            // Step 1: Release — index 10 enters free queue
            sut.ReleaseId(id1);

            // Step 2: SyncAllocatedId re-registers the same id (simulating server snapshot)
            // This clears _freeIndicesSet for index 10
            sut.SyncAllocatedId(id1);

            // Step 3: Release again (server destroyed the object)
            // Index 10 is enqueued a second time (first entry still in queue)
            sut.ReleaseId(id1);

            // First GetNext: processes first queue entry (stale from step 1).
            // _dynamicAllocatedIds no longer contains gen-1 id (removed by step 3's ReleaseId),
            // so it allocates gen 2 at index 10.
            var id2 = sut.MoveNextAndGetCurrent();
            Assert.That(NetworkIdEnumerator.ExtractIndex(id2), Is.EqualTo(10));
            Assert.That(NetworkIdEnumerator.ExtractGeneration(id2), Is.EqualTo(2));

            // Release gen-2 so the second queue entry can be tested
            sut.ReleaseId(id2);

            // Second GetNext: processes second queue entry (from step 3).
            // gen-2 id was just released, so _dynamicAllocatedIds doesn't contain it.
            // Allocates gen 3 at index 10.
            var id3 = sut.MoveNextAndGetCurrent();
            Assert.That(NetworkIdEnumerator.ExtractIndex(id3), Is.EqualTo(10));
            Assert.That(NetworkIdEnumerator.ExtractGeneration(id3), Is.EqualTo(3));
        }

        // =====================================================================
        // Bug 1 regression: free-queue gap when SyncAllocatedId races ReleaseId
        // =====================================================================

        [Test]
        public void GetNext_AfterReleaseAndSyncSameIndex_DoesNotReallocateSyncedId()
        {
            // Arrange — range [0, 99]; constructor allocates index 0 gen 1
            _sut = NetworkIdEnumerator.CreateWithRange(0, 99);
            var ctorId = _sut.GetCurrent(); // index 0, gen 1

            // Step 1: client predicts a spawn → allocates index 1 gen 1
            var predictedId = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(predictedId), Is.EqualTo(1));
            Assert.That(ExtractGeneration(predictedId), Is.EqualTo(1));

            // Step 2: client rolls back → releases index 1; it enters _freeIndices
            _sut.ReleaseId(predictedId);

            // Step 3: server re-uses index 1 gen 1 in the authoritative snapshot
            // (same index, same generation — server simply re-issued the same id)
            var serverReusedId = Encode(1, 1);
            _sut.SyncAllocatedId(serverReusedId);

            // Step 4: client calls MoveNextAndGetCurrent — must NOT dequeue index 1
            // from _freeIndices and hand it out again (that would create a collision).
            //
            // Free queue state at this point: [index 1 (live, will be skipped), index 0 (free)]
            // Expected outcome: GetNext skips index 1 (re-enqueues it), then hands out index 0 gen 2.
            _sut.ReleaseId(ctorId); // also release index 0 so free queue has [1, 0]
            var nextId = _sut.MoveNextAndGetCurrent();

            // Index 0 gen 2: free queue had [1 (skipped — live), 0 (free)] so index 0 is reused.
            Assert.That(ExtractIndex(nextId),
                Is.EqualTo(0),
                "GetNext must skip index 1 (live via SyncAllocatedId) and reuse index 0 instead");
            Assert.That(ExtractGeneration(nextId),
                Is.EqualTo(2),
                "Reused index 0 must have its generation incremented to 2");
            Assert.That(_sut.IsValid(serverReusedId),
                Is.True,
                "The server-synced id (index 1 gen 1) must still be valid after GetNext skipped it");

            // Verify no index leak: once the server releases index 1, it must become reusable.
            _sut.ReleaseId(serverReusedId); // server eventually releases index 1
            var reusedAfterRelease = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(reusedAfterRelease),
                Is.EqualTo(1),
                "Index 1 must be reusable from the free queue after the sync'd id is released — no slot leak");
            Assert.That(ExtractGeneration(reusedAfterRelease),
                Is.EqualTo(2),
                "Reused index 1 must have generation incremented to 2");
        }

        // =====================================================================
        // Bug 2 regression: double-release must not create a ghost free-queue entry
        // =====================================================================

        [Test]
        public void ReleaseId_DoubleRelease_SecondReleaseIsNoOp()
        {
            // Arrange — range [0, 99]; constructor allocates index 0 gen 1
            _sut = NetworkIdEnumerator.CreateWithRange(0, 99);
            var id = _sut.GetCurrent(); // index 0, gen 1

            // Act — release the same id twice
            _sut.ReleaseId(id);
            _sut.ReleaseId(id); // second release must be silently dropped

            // First allocation reuses index 0 gen 2 (exactly once — no ghost entry)
            var first = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(first),
                Is.EqualTo(0),
                "First allocation after double-release must reuse index 0");
            Assert.That(ExtractGeneration(first),
                Is.EqualTo(2),
                "Reused index 0 must have generation incremented to 2");

            // Second allocation must come from a fresh index, NOT index 0 gen 3
            var second = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(second),
                Is.Not.EqualTo(0),
                "Second allocation must not reuse index 0 again — ghost entry must not exist");
        }

        // =====================================================================
        // Bug 2 regression: full 8-step ghost-entry scenario
        // =====================================================================

        [Test]
        public void GetNext_GhostEntryScenario_NoGhostIndexAfterReleaseAndSync()
        {
            // Reproduces the exact sequence that triggers the double-enqueue ghost entry:
            //   1. Ctor allocates index 0 gen 1
            //   2. MoveNextAndGetCurrent → index 1 gen 1 (client prediction)
            //   3. ReleaseId(index 1 gen 1) → rollback; free queue = [1]
            //   4. SyncAllocatedId(index 1 gen 1) → server reclaims; allocated set has index 1 again
            //   5. ReleaseId(index 0 gen 1) → free queue = [1, 0]
            //   6. MoveNextAndGetCurrent → must skip index 1 (live), return index 0 gen 2
            //   7. ReleaseId(index 1 gen 1) → server releases; free queue = [1]
            //   8. MoveNextAndGetCurrent → must return index 1 gen 2 (from queue)
            //   9. MoveNextAndGetCurrent → must return fresh index 2 gen 1 (NOT index 1 gen 3)

            // Arrange — range [0, 99]; constructor allocates index 0 gen 1
            _sut = NetworkIdEnumerator.CreateWithRange(0, 99);
            var ctorId = _sut.GetCurrent(); // index 0, gen 1

            // Step 2
            var predictedId = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(predictedId), Is.EqualTo(1));
            Assert.That(ExtractGeneration(predictedId), Is.EqualTo(1));

            // Step 3 — client rollback
            _sut.ReleaseId(predictedId);

            // Step 4 — server reclaims the same id
            _sut.SyncAllocatedId(Encode(1, 1));

            // Step 5 — release ctor id; free queue is now [1, 0]
            _sut.ReleaseId(ctorId);

            // Step 6 — must skip index 1 (live) and reuse index 0 gen 2
            var step6Id = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(step6Id),
                Is.EqualTo(0),
                "Step 6: must skip live index 1 and return index 0");
            Assert.That(ExtractGeneration(step6Id),
                Is.EqualTo(2),
                "Step 6: reused index 0 must have generation 2");

            // Step 7 — server releases index 1
            _sut.ReleaseId(Encode(1, 1));

            // Step 8 — must return index 1 gen 2 from free queue
            var step8Id = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(step8Id),
                Is.EqualTo(1),
                "Step 8: index 1 must be reusable from free queue");
            Assert.That(ExtractGeneration(step8Id),
                Is.EqualTo(2),
                "Step 8: index 1 must have generation incremented to 2");

            // Step 9 — must return fresh index 2, NOT index 1 gen 3 (proving no ghost entry)
            var step9Id = _sut.MoveNextAndGetCurrent();
            Assert.That(ExtractIndex(step9Id),
                Is.EqualTo(2),
                "Step 9: must allocate fresh index 2, not a ghost re-entry of index 1");
            Assert.That(ExtractGeneration(step9Id),
                Is.EqualTo(1),
                "Step 9: fresh index 2 must start at generation 1");
        }

        // =====================================================================
        // DynamicElympicsBehaviourInstanceData — structure and equality
        // =====================================================================

        [Test]
        public void DynamicElympicsBehaviourInstanceData_Equality_SameNetworkIds_AreEqual()
        {
            var a = new DynamicElympicsBehaviourInstanceData(1, new[] { 100, 200 }, "prefab/path");
            var b = new DynamicElympicsBehaviourInstanceData(1, new[] { 100, 200 }, "prefab/path");

            Assert.That(a.Equals(b), Is.True, "Instances with same ID, NetworkIds, and InstanceType must be equal");
            Assert.That(a == b, Is.True, "== operator must return true for equal instances");
        }

        [Test]
        public void DynamicElympicsBehaviourInstanceData_Equality_DifferentNetworkIds_AreNotEqual()
        {
            var a = new DynamicElympicsBehaviourInstanceData(1, new[] { 100, 200 }, "prefab/path");
            var b = new DynamicElympicsBehaviourInstanceData(1, new[] { 100, 999 }, "prefab/path");

            Assert.That(a.Equals(b), Is.False, "Instances with different NetworkIds must not be equal");
            Assert.That(a != b, Is.True, "!= operator must return true for non-equal instances");
        }

        [Test]
        public void DynamicElympicsBehaviourInstanceData_Equality_DifferentLengths_AreNotEqual()
        {
            var a = new DynamicElympicsBehaviourInstanceData(1, new[] { 100 }, "prefab/path");
            var b = new DynamicElympicsBehaviourInstanceData(1, new[] { 100, 200 }, "prefab/path");

            Assert.That(a.Equals(b), Is.False, "Instances with different NetworkIds lengths must not be equal");
        }

        [Test]
        public void DynamicElympicsBehaviourInstanceData_GetHashCode_EqualInstances_SameHash()
        {
            var a = new DynamicElympicsBehaviourInstanceData(1, new[] { 100, 200 }, "prefab/path");
            var b = new DynamicElympicsBehaviourInstanceData(1, new[] { 100, 200 }, "prefab/path");

            Assert.That(a.GetHashCode(),
                Is.EqualTo(b.GetHashCode()),
                "Equal instances must have the same hash code");
        }

        [Test]
        public void DynamicElympicsBehaviourInstanceData_NullNetworkIds_ThrowsArgumentNullException()
        {
            _ = Assert.Throws<ArgumentNullException>(() =>
                    _ = new DynamicElympicsBehaviourInstanceData(1, null, "prefab/path"),
                "Passing null for NetworkIds must throw ArgumentNullException");
        }

        [Test]
        public void DynamicElympicsBehaviourInstanceData_NetworkIds_AreStoredCorrectly()
        {
            var networkIds = new[] { 111, 222, 333 };
            var data = new DynamicElympicsBehaviourInstanceData(42, networkIds, "some/prefab");

            Assert.That(data.ID, Is.EqualTo(42));
            Assert.That(data.NetworkIds, Is.EqualTo(networkIds));
            Assert.That(data.InstanceType, Is.EqualTo("some/prefab"));
        }

        // =====================================================================
        // MessagePack round-trip
        // =====================================================================

        [Test]
        public void DynamicElympicsBehaviourInstanceData_MessagePackRoundTrip_PreservesNetworkIds()
        {
            var original = new DynamicElympicsBehaviourInstanceData(7, new[] { 1001, 1002, 1003 }, "my/prefab");

            var bytes = MessagePack.MessagePackSerializer.Serialize(original);
            var deserialized = MessagePack.MessagePackSerializer.Deserialize<DynamicElympicsBehaviourInstanceData>(bytes);

            Assert.That(deserialized.ID, Is.EqualTo(original.ID));
            Assert.That(deserialized.InstanceType, Is.EqualTo(original.InstanceType));
            Assert.That(deserialized.NetworkIds,
                Is.EqualTo(original.NetworkIds),
                "NetworkIds must survive a MessagePack round-trip");
        }
    }
}
