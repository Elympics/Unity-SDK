using System;
using System.Collections.Generic;
using MessagePack;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Snapshots
{
    [TestFixture]
    [Category("ElympicsSnapshot")]
    public class ElympicsSnapshotStartTickTests
    {
        private static ElympicsSnapshot CreateSnapshot(long tick, long? startTick)
        {
            var snapshot = ElympicsSnapshot.CreateEmpty();
            snapshot.Tick = tick;
            snapshot.TickToPlayersInputData = new Dictionary<int, TickToPlayerInput>();
            snapshot.StartTick = startTick;
            return snapshot;
        }

        [TestCase(null)]
        [TestCase(123L)]
        public void StartTickSurvivesSerialization(long? startTick)
        {
            var bytes = MessagePackSerializer.Serialize<IFromServer>(CreateSnapshot(5, startTick));

            var deserialized = (ElympicsSnapshot)MessagePackSerializer.Deserialize<IFromServer>(bytes);

            Assert.That(deserialized.StartTick, Is.EqualTo(startTick));
        }

        [TestCase(null)]
        [TestCase(123L)]
        public void StartTickSurvivesSerializationWithMetadata(long? startTick)
        {
            var snapshot = new ElympicsSnapshotWithMetadata(CreateSnapshot(5, startTick), DateTime.UtcNow);

            var bytes = MessagePackSerializer.Serialize(snapshot);
            var deserialized = MessagePackSerializer.Deserialize<ElympicsSnapshotWithMetadata>(bytes);

            Assert.That(deserialized.StartTick, Is.EqualTo(startTick));
            Assert.That(deserialized.Tick, Is.EqualTo(5));
        }

        [Test]
        public void LegacyFiveElementSnapshotDeserializesWithoutStartTick()
        {
            var bytes = MessagePackSerializer.Serialize(CreateSnapshot(5, 123));
            var reader = new MessagePackReader(bytes);
            _ = reader.ReadArrayHeader();
            var start = reader.Position;
            for (var i = 0; i < 5; i++)
                reader.Skip();
            var firstFive = reader.Sequence.Slice(start, reader.Position);
            var legacyBytes = new byte[1 + firstFive.Length];
            legacyBytes[0] = 0x95; // fixarray of 5 elements, as written before StartTick existed
            var offset = 1;
            foreach (var segment in firstFive)
            {
                segment.Span.CopyTo(legacyBytes.AsSpan(offset));
                offset += segment.Length;
            }

            var deserialized = MessagePackSerializer.Deserialize<ElympicsSnapshot>(legacyBytes);

            Assert.That(deserialized.Tick, Is.EqualTo(5));
            Assert.That(deserialized.StartTick, Is.Null);
        }

        [Test]
        public void DeepCopyAndCopyConstructorKeepStartTick()
        {
            var snapshot = CreateSnapshot(5, 40);

            Assert.That(ElympicsSnapshot.CreateDeepCopy(snapshot).StartTick, Is.EqualTo(40));
            Assert.That(new ElympicsSnapshotWithMetadata(snapshot, DateTime.UtcNow).StartTick, Is.EqualTo(40));
        }

        [Test]
        public void ResetToEmptyClearsStartTick()
        {
            var snapshot = CreateSnapshot(5, 40);

            snapshot.ResetToEmpty();

            Assert.That(snapshot.StartTick, Is.Null);
        }

        [Test]
        public void MergeTakesReceivedStartTickAndKeepsKnownOneOtherwise()
        {
            var state = CreateSnapshot(1, null);

            state.MergeWithSnapshot(CreateSnapshot(2, 40));
            Assert.That(state.StartTick, Is.EqualTo(40));

            state.MergeWithSnapshot(CreateSnapshot(3, null));
            Assert.That(state.StartTick, Is.EqualTo(40));
        }
    }
}
