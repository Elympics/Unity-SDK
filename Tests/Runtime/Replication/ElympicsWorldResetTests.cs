using System;
using System.Collections.Generic;
using Elympics.Replication;
using NUnit.Framework;

namespace Elympics.Tests.Runtime.Replication
{
    [TestFixture]
    [Category("Replication")]
    public class ElympicsWorldResetTests
    {
        private ElympicsWorld _sut;

        [SetUp]
        public void SetUp()
        {
            ElympicsWorld.Current = null;
            ReplicationPipeline.Current = null;
            _sut = null;
        }

        [TearDown]
        public void TearDown()
        {
            ReplicationPipeline.Current?.Dispose();
            _sut?.Dispose();
            ElympicsWorld.Current = null;
            ReplicationPipeline.Current = null;
        }

        private static ElympicsSnapshot CreateSnapshot(long tick)
        {
            return new ElympicsSnapshot(
                tick,
                DateTime.UtcNow,
                new FactoryState(new Dictionary<int, FactoryPartState>()),
                new Dictionary<int, byte[]>(),
                null);
        }

        [Test]
        public void Reset_ClearsDenseArrays()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            for (var i = 0; i < 5; i++)
                _sut.RegisterEntity(i, ElympicsPlayer.All);
            Assert.That(_sut.DenseCount, Is.EqualTo(5));

            // Act
            _sut.Reset();

            // Assert
            Assert.That(_sut.DenseCount, Is.EqualTo(0));
            for (var i = 0; i < 5; i++)
                Assert.That(_sut.SparseToDense[i], Is.EqualTo(-1));
        }

        [Test]
        public void Reset_ClearsPlayerState()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.ActivatePlayer(0, ElympicsPlayer.FromIndex(0));
            _sut.ActivatePlayer(1, ElympicsPlayer.FromIndex(1));
            Assert.That(_sut.ActivePlayersCount, Is.EqualTo(2));

            // Act
            _sut.Reset();

            // Assert
            Assert.That(_sut.ActivePlayersCount, Is.EqualTo(0));
            Assert.That(_sut.PlayerIds[0], Is.EqualTo(ElympicsPlayer.Invalid));
            Assert.That(_sut.PlayerIds[1], Is.EqualTo(ElympicsPlayer.Invalid));
            Assert.That(_sut.PlayerLastReceivedSnapshot[0], Is.EqualTo(-1));
            Assert.That(_sut.PlayerLastReceivedSnapshot[1], Is.EqualTo(-1));
        }

        [Test]
        public void Reset_ClearsSnapshotsAndTick()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            var snapshot = CreateSnapshot(42);
            _sut.BeginTick(snapshot, 42);
            Assert.That(_sut.CurrentTick, Is.EqualTo(42));

            // Act
            _sut.Reset();

            // Assert — both snapshots reset to empty (null object pattern)
            Assert.That(_sut.CurrentSnapshot.Tick, Is.EqualTo(-1));
            Assert.That(_sut.CurrentSnapshot.Data, Is.Empty);
            Assert.That(_sut.CurrentSnapshot.Factory.Parts, Is.Empty);
            Assert.That(_sut.PreviousSnapshot.Tick, Is.EqualTo(-1));
            Assert.That(_sut.PreviousSnapshot.Data, Is.Empty);
            Assert.That(_sut.PreviousSnapshot.Factory.Parts, Is.Empty);
            Assert.That(_sut.CurrentTick, Is.EqualTo(-1));
            Assert.That(_sut.CurrentSnapshot, Is.Not.SameAs(_sut.PreviousSnapshot));
        }

        [Test]
        public void Reset_AllowsReRegistration()
        {
            // Arrange
            _sut = new ElympicsWorld(4, 256, 512);
            _sut.RegisterEntity(10, ElympicsPlayer.All);
            Assert.That(_sut.DenseCount, Is.EqualTo(1));

            // Act
            _sut.Reset();
            _sut.RegisterEntity(10, ElympicsPlayer.All);

            // Assert
            Assert.That(_sut.DenseCount, Is.EqualTo(1));
            Assert.That(_sut.SparseToDense[10], Is.EqualTo(0));
        }

        [Test]
        public void Reset_PreservesArrayAllocations()
        {
            // Arrange — register enough to trigger a grow
            _sut = new ElympicsWorld(4, 256, 512);
            for (var i = 0; i < 129; i++)
                _sut.RegisterEntity(i, ElympicsPlayer.All);
            Assert.That(_sut.DenseCapacity, Is.GreaterThanOrEqualTo(129));

            // Act
            _sut.Reset();

            // Assert — arrays should still be allocated (no shrink below initial)
            Assert.That(_sut.DenseCount, Is.EqualTo(0));
            Assert.That(_sut.SparseToDense, Is.Not.Null);
            Assert.That(_sut.DenseToSparse, Is.Not.Null);
        }
    }
}
