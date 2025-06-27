using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Elympics.SnapshotAnalysis;
using Elympics.SnapshotAnalysis.Serialization;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Elympics.Tests.SnapshotAnalysis
{
    public class SnapshotDeserializerTests
    {
        private LatestMessagePackSerializer _sut = new();

        private static readonly int[] Count = { 0, 1, 2, 3, 5, 257 };
        [UnityTest]
        public IEnumerator Deserialize([ValueSource(nameof(Count))] int count) => UniTask.ToCoroutine(async () =>
        {
            MemoryStream stream = new();
            Dictionary<long, ElympicsSnapshotWithMetadata> snapshots = new(count);
            var initData = CreateTestInitData();

            for (var i = 0; i < count; i++)
                snapshots.Add(i, CreateTestSnapshot(i));

            await _sut.SerializeVersionToStream(stream, SerializationUtil.LatestVersion);
            await _sut.SerializeToStream(stream, initData);

            for (var i = 0; i < count; i++)
                await _sut.SerializeToStream(stream,
                    new SnapshotSerializationPackage
                    {
                        Snapshots = new ElympicsSnapshotWithMetadata[]
                            { snapshots[i] }
                    });

            stream.Position = 0;
            var deserialized = SnapshotDeserializer.DeserializeSnapshots(stream);

            Assert.AreEqual(initData, deserialized.InitData);
            Assert.AreEqual(snapshots.Count, deserialized.Snapshots.Count);
            CollectionAssert.AreEqual(snapshots.Keys, deserialized.Snapshots.Keys);

            foreach (var (a, b) in snapshots.Values.Zip(deserialized.Snapshots.Values, (x, y) => (x, y)))
                AssertSnapshotsAreEqual(a, b);
        });

        private static void AssertSnapshotsAreEqual(ElympicsSnapshotWithMetadata snapshot, ElympicsSnapshotWithMetadata retrievedSnapshot)
        {
            Assert.AreEqual(snapshot.Tick, retrievedSnapshot.Tick);
            Assert.AreEqual(snapshot.TickStartUtc, retrievedSnapshot.TickStartUtc);
            Assert.AreEqual(snapshot.TickEndUtc, retrievedSnapshot.TickEndUtc);
            CollectionAssert.AreEqual(snapshot.Data, retrievedSnapshot.Data);
            CollectionAssert.AreEqual(snapshot.Metadata, retrievedSnapshot.Metadata, new ElympicsBehaviourMetadataComparer());
            CollectionAssert.AreEqual(snapshot.Factory.Parts, retrievedSnapshot.Factory.Parts);
        }

        private static SnapshotSaverInitData CreateTestInitData() => new("test", "test", "test", "test", 6, "test", 30f, default, null, new List<InitialMatchPlayerDataGuid>());

        private static ElympicsSnapshotWithMetadata CreateTestSnapshot(long tick)
        {
            return new ElympicsSnapshotWithMetadata
            {
                Tick = tick,
                TickStartUtc = DateTime.UtcNow,
                Factory = new() { Parts = new() { new(27, new byte[] { 1, 2, 3 }), new(28, new byte[] { 1, 2, 3 }) } },
                Data = new() { new(27, new byte[] { 1, 2, 3 }), new(28, new byte[] { 1, 2, 3 }) },
                TickEndUtc = DateTime.UtcNow,
                Metadata = new()
                {
                    new() { Name = "test", NetworkId = 27, PredictableFor = ElympicsPlayer.All, PrefabName = "test", StateMetadata = new() { ("test", new() { ("test", "test") }) } },
                    new() { Name = "test", NetworkId = 28, PredictableFor = ElympicsPlayer.All, PrefabName = "test", StateMetadata = new() { ("test", new() { ("test", "test") }) } }
                }
            };
        }

        private class ElympicsBehaviourMetadataComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                var a = (ElympicsBehaviourMetadata)x;
                var b = (ElympicsBehaviourMetadata)x;

                var equal = a.Name == b.Name;
                equal &= a.NetworkId == b.NetworkId;
                equal &= a.PredictableFor == b.PredictableFor;
                equal &= a.PrefabName == b.PrefabName;

                var aHasState = a.StateMetadata != null;
                var bHasState = b.StateMetadata != null;

                equal &= aHasState == bHasState;
                equal &= a.StateMetadata.Count == b.StateMetadata.Count;

                if (!equal)
                    return 1;

                if (aHasState && bHasState)
                {
                    for (var i = 0; i < a.StateMetadata.Count; i++)
                    {
                        var aa = a.StateMetadata[i];
                        var bb = b.StateMetadata[i];
                        equal &= aa.Item1 == bb.Item1;
                        equal &= aa.Item2.Count == bb.Item2.Count;

                        if (!equal)
                            return 1;

                        for (var j = 0; j < aa.Item2.Count; j++)
                        {
                            equal &= aa.Item2[j] == bb.Item2[j];
                        }
                    }
                }

                return equal ? 0 : 1;
            }
        }
    }
}
