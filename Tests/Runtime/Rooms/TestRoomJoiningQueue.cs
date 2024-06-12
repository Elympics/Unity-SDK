using System;
using System.Collections.Generic;
using NUnit.Framework;

#nullable enable

namespace Elympics.Tests.Rooms
{
    [Category("Rooms")]
    internal class TestRoomJoiningQueue
    {
        private RoomJoiningQueue _sut = new();

        [SetUp]
        public void ResetSut() => _sut = new RoomJoiningQueue();

        private static List<string[]> differentJoinCodesTestCases = new()
        {
            new[] { "abcdef" },
            new[] { "abcdef", "ABCDEF" },
            new[] { "abcdef", "ABCDEF", "123456" },
        };

        [Test]
        public void AddingDifferentJoinCodesToEmptyQueueShouldSucceed([ValueSource(nameof(differentJoinCodesTestCases))] string[] joinCodes)
        {
            foreach (var joinCode in joinCodes)
                _ = _sut.AddJoinCode(joinCode);
        }

        [Test]
        public void AddingTheSameJoinCodeTwiceShouldResultInException()
        {
            const string joinCode = "abcdef";
            _ = _sut.AddJoinCode(joinCode);

            Assert.That(() => _ = _sut.AddJoinCode(joinCode),
                Throws.InstanceOf<RoomAlreadyJoinedException>().With.Message.Contains(joinCode));
        }

        [Test]
        public void AddingTheSameJoinCodeShouldBePossibleAfterTheFirstEntryIsDisposed()
        {
            const string joinCode = "abcdef";
            var entry = _sut.AddJoinCode(joinCode);
            entry.Dispose();

            _ = _sut.AddJoinCode(joinCode);
        }

        [Test]
        public void CopyOfTheDisposableEntryShouldRetainItsPropertiesForJoinCode()
        {
            const string joinCode = "abcdef";
            var entry = _sut.AddJoinCode(joinCode);
            var copiedEntry = entry;
            copiedEntry.Dispose();

            _ = _sut.AddJoinCode(joinCode);
        }

        [Test]
        public void AddingTheSameJoinCodeShouldBePossibleAfterTheQueueIsCleared()
        {
            const string joinCode = "abcdef";
            _ = _sut.AddJoinCode(joinCode);
            _sut.Clear();

            _ = _sut.AddJoinCode(joinCode);
        }

        [Test]
        public void DisposingJoinCodeShouldHaveNoEffectAfterTheQueueIsCleared()
        {
            const string joinCode = "abcdef";
            var entry = _sut.AddJoinCode(joinCode);
            _sut.Clear();
            _ = _sut.AddJoinCode(joinCode);
            entry.Dispose();

            Assert.That(() => _ = _sut.AddJoinCode(joinCode),
                Throws.InstanceOf<RoomAlreadyJoinedException>().With.Message.Contains(joinCode));
        }

        private static List<Guid[]> differentRoomIdsTestCases = new()
        {
            new[] { new Guid("d060f00d-0000-0000-0000-1d0000000001") },
            new[] { new Guid("d060f00d-0000-0000-0000-1d0000000001"), new Guid("d060f00d-0000-0000-0000-1d0000000002") },
            new[] { new Guid("d060f00d-0000-0000-0000-1d0000000001"), new Guid("d060f00d-0000-0000-0000-1d0000000002"), new Guid("d060f00d-0000-0000-0000-1d0000000003") },
        };

        [Test]
        public void AddingDifferentRoomIdsToEmptyQueueShouldSucceed([ValueSource(nameof(differentRoomIdsTestCases))] Guid[] roomIds)
        {
            foreach (var roomId in roomIds)
                _ = _sut.AddRoomId(roomId);
        }

        [Test]
        public void AddingTheSameRoomIdTwiceShouldResultInException()
        {
            var roomId = new Guid("d060f00d-0000-0000-0000-1d0000000001");
            _ = _sut.AddRoomId(roomId);

            Assert.That(() => _ = _sut.AddRoomId(roomId),
                Throws.InstanceOf<RoomAlreadyJoinedException>().With.Message.Contains(roomId.ToString()));
        }

        [Test]
        public void AddingTheSameRoomIdShouldBePossibleAfterTheFirstEntryIsDisposed()
        {
            var roomId = new Guid("d060f00d-0000-0000-0000-1d0000000001");
            var entry = _sut.AddRoomId(roomId);
            entry.Dispose();

            _ = _sut.AddRoomId(roomId);
        }

        [Test]
        public void CopyOfTheDisposableEntryShouldRetainItsPropertiesForRoomId()
        {
            var roomId = new Guid("d060f00d-0000-0000-0000-1d0000000001");
            var entry = _sut.AddRoomId(roomId);
            var copiedEntry = entry;
            copiedEntry.Dispose();

            _ = _sut.AddRoomId(roomId);
        }

        [Test]
        public void AddingTheSameRoomIdShouldBePossibleAfterTheQueueIsCleared()
        {
            var roomId = new Guid("d060f00d-0000-0000-0000-1d0000000001");
            _ = _sut.AddRoomId(roomId);
            _sut.Clear();

            _ = _sut.AddRoomId(roomId);
        }

        [Test]
        public void DisposingRoomIdShouldHaveNoEffectAfterTheQueueIsCleared()
        {
            var roomId = new Guid("d060f00d-0000-0000-0000-1d0000000001");
            var entry = _sut.AddRoomId(roomId);
            _sut.Clear();
            _ = _sut.AddRoomId(roomId);
            entry.Dispose();

            Assert.That(() => _ = _sut.AddRoomId(roomId),
                Throws.InstanceOf<RoomAlreadyJoinedException>().With.Message.Contains(roomId.ToString()));
        }

        [Test]
        public void AddingRoomIdAndJoinCodeOfSimilarValueShouldSucceed()
        {
            const string joinCode = "d060f00d-0000-0000-0000-1d0000000001";
            var roomId = new Guid(joinCode);

            _ = _sut.AddJoinCode(joinCode);
            _ = _sut.AddRoomId(roomId);
        }
    }
}
