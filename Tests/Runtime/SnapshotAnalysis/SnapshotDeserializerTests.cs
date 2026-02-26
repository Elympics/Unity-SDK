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
            var tickStartUtc = DateTime.UtcNow;
            var tickEndUtc = DateTime.UtcNow;
            return new ElympicsSnapshotWithMetadata(
                tick,
                tickStartUtc,
                new
                (
                    new()
                    {
                        { 27, new(29, new(1, new() { { 30, new(31, new[] { 32 }, "tst1") } } )) },
                        { 33, new(34, new(1, new() { { 35, new(36, new[] { 37 }, "tst2") } } )) },
                    }
                ),
                new() { { 27, new byte[] { 1, 2, 3 } }, { 28, new byte[] { 1, 2, 3 } } },
                null,
                tickEndUtc,
                new()
                {
                    new() { Name = "test", NetworkId = 27, PredictableFor = ElympicsPlayer.All, PrefabName = "test"},
                    new() { Name = "test", NetworkId = 28, PredictableFor = ElympicsPlayer.All, PrefabName = "test"}
                });
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


                if (!equal)
                    return 1;

                return equal ? 0 : 1;
            }
        }
    }
}
