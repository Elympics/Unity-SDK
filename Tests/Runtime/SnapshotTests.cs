using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

#nullable enable

namespace Elympics.Tests
{
    [TestFixture]
    public class SnapshotTests
    {
        [TestCase(new[] { 1, 3, 4, 7 }, new[] { 1, 2, 3, 4, 5, 6, 7 })]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7 }, new[] { 1, 2, 3, 4, 5, 6, 7 })]
        [TestCase(new[] { 7 }, new[] { 1, 7 })]
        [TestCase(new[] { 7 }, new[] { 7 })]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6 }, new[] { 1, 2, 3, 4, 5, 7 })]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7 }, new[] { 1, 2, 3, 4, 5, 7 })]
        public void FillMissingFrom(int[] initialNetworkIds, int[] newNetworkIds)
        {
            // "server" snapshot that might not contain all objects
            var snapshot = new ElympicsSnapshot
            {
                Data = initialNetworkIds.Select(networkId => new KeyValuePair<int, byte[]>(networkId, Array.Empty<byte>())).ToList(),
            };

            // "local" snapshot that has data of all objects
            var source = new ElympicsSnapshot
            {
                Data = newNetworkIds.Select(networkId => new KeyValuePair<int, byte[]>(networkId, Array.Empty<byte>())).ToList(),
            };

            snapshot.FillMissingFrom(source);

            var expected = new HashSet<int>(initialNetworkIds.Concat(newNetworkIds));

            CollectionAssert.AreEquivalent(expected, snapshot.Data.Select(kvp => kvp.Key));
        }

        [Test]
        public void MergeWithSnapshot_WithNullSnapshot_DoesNothing()
        {
            var currentSnapshot = new ElympicsSnapshot
            {
                Tick = 10,
                TickStartUtc = DateTime.UtcNow,
                Data = new List<KeyValuePair<int, byte[]>> { new(1, new byte[] { 1, 2, 3 }) },
            };

            var originalTick = currentSnapshot.Tick;
            var originalData = currentSnapshot.Data;

            currentSnapshot.MergeWithSnapshot(null);

            Assert.That(currentSnapshot.Tick, Is.EqualTo(originalTick));
            Assert.That(currentSnapshot.Data, Is.SameAs(originalData));
        }

        [Test]
        public void MergeWithSnapshot_WithNullCurrentData_CopiesReceivedData()
        {
            var currentSnapshot = new ElympicsSnapshot
            {
                Tick = 10,
                Data = null,
            };

            var receivedSnapshot = new ElympicsSnapshot
            {
                Tick = 20,
                TickStartUtc = DateTime.UtcNow,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(1, new byte[] { 1 }),
                    new(2, new byte[] { 2 }),
                },
            };

            currentSnapshot.MergeWithSnapshot(receivedSnapshot);

            Assert.That(currentSnapshot.Tick, Is.EqualTo(20));
            Assert.That(currentSnapshot.Data, Is.Not.Null);
            Assert.That(currentSnapshot.Data.Count, Is.EqualTo(2));
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, currentSnapshot.Data.Select(kvp => kvp.Key));
        }

        [Test]
        public void MergeWithSnapshot_UpdatesBasicFields()
        {
            var currentSnapshot = new ElympicsSnapshot
            {
                Tick = 10,
                TickStartUtc = DateTime.UtcNow,
                Factory = new FactoryState { Parts = new() { new(2, new() { currentNetworkId = 29, dynamicInstancesState = new() { instancesCounter = 1, instances = new() { { 30, new(31, 32, "tst1") } } } }) } },
                TickToPlayersInputData = new Dictionary<int, TickToPlayerInput>(),
                Data = new List<KeyValuePair<int, byte[]>>(),
            };

            var newTickStartUtc = DateTime.UtcNow.AddSeconds(10);
            var receivedSnapshot = new ElympicsSnapshot
            {
                Tick = 20,
                TickStartUtc = newTickStartUtc,
                Factory = new FactoryState { Parts = new() { new(2, new() { currentNetworkId = 29, dynamicInstancesState = new() { instancesCounter = 1, instances = new() { { 30, new(31, 32, "tst1") } } } }), } },
                TickToPlayersInputData = new Dictionary<int, TickToPlayerInput> { { 0, new TickToPlayerInput() } },
                Data = new List<KeyValuePair<int, byte[]>>(),
            };

            currentSnapshot.MergeWithSnapshot(receivedSnapshot);

            Assert.That(currentSnapshot.Tick, Is.EqualTo(20));
            Assert.That(currentSnapshot.TickStartUtc, Is.EqualTo(newTickStartUtc));
            Assert.That(currentSnapshot.Factory.Parts.Count, Is.EqualTo(1));
            Assert.That(currentSnapshot.Factory.Parts[0].Key, Is.EqualTo(2));
            Assert.That(currentSnapshot.TickToPlayersInputData.Count, Is.EqualTo(1));
        }

        [Test]
        public void MergeWithSnapshot_UpdatesExistingNetworkIds()
        {
            var currentSnapshot = new ElympicsSnapshot
            {
                Tick = 10,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(1, new byte[] { 1, 1 }),
                    new(2, new byte[] { 2, 2 }),
                    new(3, new byte[] { 3, 3 }),
                },
            };

            var receivedSnapshot = new ElympicsSnapshot
            {
                Tick = 20,
                TickStartUtc = DateTime.UtcNow,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(1, new byte[] { 10, 10 }),
                    new(3, new byte[] { 30, 30 }),
                },
            };

            currentSnapshot.MergeWithSnapshot(receivedSnapshot);

            Assert.That(currentSnapshot.Data.Count, Is.EqualTo(3));

            var dataDict = currentSnapshot.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            Assert.That(dataDict[1], Is.EqualTo(new byte[] { 10, 10 }));
            Assert.That(dataDict[2], Is.EqualTo(new byte[] { 2, 2 })); // Unchanged
            Assert.That(dataDict[3], Is.EqualTo(new byte[] { 30, 30 }));
        }

        [Test]
        public void MergeWithSnapshot_AddsNewNetworkIds()
        {
            var currentSnapshot = new ElympicsSnapshot
            {
                Tick = 10,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(1, new byte[] { 1 }),
                    new(2, new byte[] { 2 }),
                },
            };

            var receivedSnapshot = new ElympicsSnapshot
            {
                Tick = 20,
                TickStartUtc = DateTime.UtcNow,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(3, new byte[] { 3 }),
                    new(4, new byte[] { 4 }),
                },
            };

            currentSnapshot.MergeWithSnapshot(receivedSnapshot);

            Assert.That(currentSnapshot.Data.Count, Is.EqualTo(4));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, currentSnapshot.Data.Select(kvp => kvp.Key));
        }

        [Test]
        public void MergeWithSnapshot_CoreFunctionality_InsertsNewReplacesExistingKeepsUntouched()
        {
            // MAIN TEST: Verifies all three core behaviors in one comprehensive test
            // Current snapshot has network IDs: 1, 2, 3, 4
            var currentSnapshot = new ElympicsSnapshot
            {
                Tick = 10,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(1, new byte[] { 1, 1, 1 }),     // Will stay UNTOUCHED
                    new(2, new byte[] { 2, 2, 2 }),     // Will be REPLACED
                    new(3, new byte[] { 3, 3, 3 }),     // Will stay UNTOUCHED
                    new(4, new byte[] { 4, 4, 4 }),     // Will be REPLACED
                },
            };

            // Received snapshot has network IDs: 2, 4, 5, 6
            var receivedSnapshot = new ElympicsSnapshot
            {
                Tick = 20,
                TickStartUtc = DateTime.UtcNow,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(2, new byte[] { 20, 20, 20 }), // REPLACE existing ID 2
                    new(4, new byte[] { 40, 40, 40 }), // REPLACE existing ID 4
                    new(5, new byte[] { 50, 50, 50 }), // INSERT new ID 5
                    new(6, new byte[] { 60, 60, 60 }), // INSERT new ID 6
                },
            };

            currentSnapshot.MergeWithSnapshot(receivedSnapshot);

            // Verify total count: 2 untouched + 2 replaced + 2 inserted = 6
            Assert.That(currentSnapshot.Data.Count, Is.EqualTo(6), "Should have all network IDs: 2 untouched, 2 replaced, 2 inserted");

            // Verify all expected network IDs are present
            var networkIds = currentSnapshot.Data.Select(kvp => kvp.Key).ToArray();
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, networkIds, "Should contain all network IDs");

            var dataDict = currentSnapshot.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // KEEP UNTOUCHED: IDs that exist in current but not in received should remain unchanged
            Assert.That(dataDict[1], Is.EqualTo(new byte[] { 1, 1, 1 }), "Network ID 1 should remain UNTOUCHED (not in received snapshot)");
            Assert.That(dataDict[3], Is.EqualTo(new byte[] { 3, 3, 3 }), "Network ID 3 should remain UNTOUCHED (not in received snapshot)");

            // REPLACE EXISTING: IDs that exist in both should be replaced with received values
            Assert.That(dataDict[2], Is.EqualTo(new byte[] { 20, 20, 20 }), "Network ID 2 should be REPLACED with received value");
            Assert.That(dataDict[4], Is.EqualTo(new byte[] { 40, 40, 40 }), "Network ID 4 should be REPLACED with received value");

            // INSERT NEW: IDs that exist only in received should be added
            Assert.That(dataDict[5], Is.EqualTo(new byte[] { 50, 50, 50 }), "Network ID 5 should be INSERTED (new from received snapshot)");
            Assert.That(dataDict[6], Is.EqualTo(new byte[] { 60, 60, 60 }), "Network ID 6 should be INSERTED (new from received snapshot)");
        }

        [Test]
        public void MergeWithSnapshot_KeepsUntouchedData_WhenReceivedHasOnlyNewIds()
        {
            // Focus on verifying UNTOUCHED behavior
            var currentSnapshot = new ElympicsSnapshot
            {
                Tick = 10,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(1, new byte[] { 100, 101, 102 }),
                    new(2, new byte[] { 200, 201, 202 }),
                    new(3, new byte[] { 250, 251, 252 }),
                },
            };

            // Received snapshot has completely different network IDs (no overlap)
            var receivedSnapshot = new ElympicsSnapshot
            {
                Tick = 20,
                TickStartUtc = DateTime.UtcNow,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(10, new byte[] { 10 }),
                    new(20, new byte[] { 20 }),
                },
            };

            currentSnapshot.MergeWithSnapshot(receivedSnapshot);

            // All original data should remain untouched
            Assert.That(currentSnapshot.Data.Count, Is.EqualTo(5), "Should have all original + new data");

            var dataDict = currentSnapshot.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Verify original data is UNTOUCHED (exact same byte arrays)
            Assert.That(dataDict[1], Is.EqualTo(new byte[] { 100, 101, 102 }), "Original network ID 1 data should be UNTOUCHED");
            Assert.That(dataDict[2], Is.EqualTo(new byte[] { 200, 201, 202 }), "Original network ID 2 data should be UNTOUCHED");
            Assert.That(dataDict[3], Is.EqualTo(new byte[] { 250, 251, 252 }), "Original network ID 3 data should be UNTOUCHED");

            // Verify new data was added
            Assert.That(dataDict[10], Is.EqualTo(new byte[] { 10 }), "New network ID 10 should be added");
            Assert.That(dataDict[20], Is.EqualTo(new byte[] { 20 }), "New network ID 20 should be added");
        }

        [Test]
        public void MergeWithSnapshot_ReplacesAllMatchingIds_CompleteOverlap()
        {
            // Focus on verifying REPLACE behavior
            var currentSnapshot = new ElympicsSnapshot
            {
                Tick = 10,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(1, new byte[] { 1, 2, 3 }),
                    new(2, new byte[] { 4, 5, 6 }),
                    new(3, new byte[] { 7, 8, 9 }),
                },
            };

            // Received snapshot has all the same network IDs but with different data
            var receivedSnapshot = new ElympicsSnapshot
            {
                Tick = 20,
                TickStartUtc = DateTime.UtcNow,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(1, new byte[] { 111 }),
                    new(2, new byte[] { 222 }),
                    new(3, new byte[] { 233 }),
                },
            };

            currentSnapshot.MergeWithSnapshot(receivedSnapshot);

            Assert.That(currentSnapshot.Data.Count, Is.EqualTo(3), "Should have same number of entries");

            var dataDict = currentSnapshot.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // All data should be REPLACED with received values
            Assert.That(dataDict[1], Is.EqualTo(new byte[] { 111 }), "Network ID 1 should be REPLACED");
            Assert.That(dataDict[2], Is.EqualTo(new byte[] { 222 }), "Network ID 2 should be REPLACED");
            Assert.That(dataDict[3], Is.EqualTo(new byte[] { 233 }), "Network ID 3 should be REPLACED");

            // Verify old values are NOT present
            Assert.That(dataDict[1], Is.Not.EqualTo(new byte[] { 1, 2, 3 }), "Old value should be gone");
            Assert.That(dataDict[2], Is.Not.EqualTo(new byte[] { 4, 5, 6 }), "Old value should be gone");
            Assert.That(dataDict[3], Is.Not.EqualTo(new byte[] { 7, 8, 9 }), "Old value should be gone");
        }

        [Test]
        public void MergeWithSnapshot_WithEmptyReceivedData_KeepsCurrentData()
        {
            var currentSnapshot = new ElympicsSnapshot
            {
                Tick = 10,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(1, new byte[] { 1 }),
                    new(2, new byte[] { 2 }),
                },
            };

            var receivedSnapshot = new ElympicsSnapshot
            {
                Tick = 20,
                TickStartUtc = DateTime.UtcNow,
                Data = new List<KeyValuePair<int, byte[]>>(),
            };

            currentSnapshot.MergeWithSnapshot(receivedSnapshot);

            Assert.That(currentSnapshot.Tick, Is.EqualTo(20));
            Assert.That(currentSnapshot.Data.Count, Is.EqualTo(2));
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, currentSnapshot.Data.Select(kvp => kvp.Key));
        }

        [Test]
        public void MergeWithSnapshot_WithNullReceivedData_DoesNotModifyCurrentData()
        {
            var currentSnapshot = new ElympicsSnapshot
            {
                Tick = 10,
                Data = new List<KeyValuePair<int, byte[]>>
                {
                    new(1, new byte[] { 1 }),
                },
            };

            var receivedSnapshot = new ElympicsSnapshot
            {
                Tick = 20,
                TickStartUtc = DateTime.UtcNow,
                Data = null,
            };

            currentSnapshot.MergeWithSnapshot(receivedSnapshot);

            Assert.That(currentSnapshot.Tick, Is.EqualTo(20));
            Assert.That(currentSnapshot.Data, Is.Not.Null);
            Assert.That(currentSnapshot.Data.Count, Is.EqualTo(1));
        }
    }
}
