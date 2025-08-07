using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Elympics.SnapshotAnalysis;
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


        private class TestSnapshotAnalysisCollector : SnapshotAnalysisCollector
        {
            public override void CaptureSnapshot(ElympicsSnapshotWithMetadata? previousSnapshot, ElympicsSnapshotWithMetadata snapshot) =>
                StoreToBuffer(previousSnapshot, snapshot);

            public ElympicsSnapshotWithMetadata[] Buffer => GetBuffer;

            protected override void SaveInitData(SnapshotSaverInitData initData) => throw new NotImplementedException();
            protected override UniTaskVoid OnBufferLimit(ElympicsSnapshotWithMetadata[] buffer) => throw new NotImplementedException();
            protected override void SaveLastDataAndDispose(ElympicsSnapshotWithMetadata[] snapshots) => throw new NotImplementedException();
        }

        public record CapturedSnapshotTestCase(ElympicsSnapshotWithMetadata? Previous, ElympicsSnapshotWithMetadata Current);

        private static IEnumerable<CapturedSnapshotTestCase> capturedSnapshotTestCases = new[]
        {
            new CapturedSnapshotTestCase(null, new ElympicsSnapshotWithMetadata
            {
                Tick = 1,
                TickStartUtc = DateTime.UtcNow,
                Factory = new FactoryState { Parts = new List<KeyValuePair<int, byte[]>>() },
                Data = new List<KeyValuePair<int, byte[]>> { new(1, Array.Empty<byte>()) },
                TickToPlayersInputData = new Dictionary<int, TickToPlayerInput>(),
                TickEndUtc = DateTime.UtcNow + TimeSpan.FromMilliseconds(10),
                Metadata = new List<ElympicsBehaviourMetadata>
                {
                    new()
                    {
                        Name = "Object #1",
                        NetworkId = 1,
                        PredictableFor = ElympicsPlayer.World,
                        PrefabName = string.Empty,
                        StateMetadata = new List<(string, List<(string, string)>)>
                        {
                            ("Component #1", new List<(string, string)>
                            {
                                ("Variable #1", ""),
                                ("Variable #2", ""),
                            }),
                        },
                    },
                },
            }),
            new CapturedSnapshotTestCase(new ElympicsSnapshotWithMetadata
            {
                Tick = 1,
                TickStartUtc = DateTime.UtcNow,
                Factory = new FactoryState { Parts = new List<KeyValuePair<int, byte[]>>() },
                Data = new List<KeyValuePair<int, byte[]>> { new(1, Array.Empty<byte>()) },
                TickToPlayersInputData = new Dictionary<int, TickToPlayerInput>(),
                TickEndUtc = DateTime.UtcNow + TimeSpan.FromMilliseconds(10),
                Metadata = new List<ElympicsBehaviourMetadata>
                {
                    new()
                    {
                        Name = "Object #1",
                        NetworkId = 1,
                        PredictableFor = ElympicsPlayer.World,
                        PrefabName = string.Empty,
                        StateMetadata = new List<(string, List<(string, string)>)>
                        {
                            ("Component #1", new List<(string, string)>
                            {
                                ("Variable #1", ""),
                                ("Variable #2", ""),
                            }),
                        },
                    },
                },
            }, new ElympicsSnapshotWithMetadata
            {
                Tick = 2,
                TickStartUtc = DateTime.UtcNow,
                Factory = new FactoryState { Parts = new List<KeyValuePair<int, byte[]>>() },
                Data = new List<KeyValuePair<int, byte[]>> { new(1, Array.Empty<byte>()) },
                TickToPlayersInputData = new Dictionary<int, TickToPlayerInput>(),
                TickEndUtc = DateTime.UtcNow + TimeSpan.FromMilliseconds(10),
                Metadata = new List<ElympicsBehaviourMetadata>
                {
                    new()
                    {
                        Name = "Object #1",
                        NetworkId = 1,
                        PredictableFor = ElympicsPlayer.World,
                        PrefabName = string.Empty,
                        StateMetadata = new List<(string, List<(string, string)>)>
                        {
                            ("Component #1", new List<(string, string)>
                            {
                                ("Variable #1", ""),
                                ("Variable #2", ""),
                            }),
                        },
                    },
                },
            }),
        };

        [Test]
        public void CapturedSnapshotsShouldHaveIntactMetadata([ValueSource(nameof(capturedSnapshotTestCases))] CapturedSnapshotTestCase testCase)
        {
            var collector = new TestSnapshotAnalysisCollector();
            var buffer = collector.Buffer;

            collector.CaptureSnapshot(testCase.Previous, testCase.Current);

            Assert.That(buffer, Is.Not.Empty);
            Assert.That(buffer[0].Metadata, Is.Not.Empty);
        }
    }
}
