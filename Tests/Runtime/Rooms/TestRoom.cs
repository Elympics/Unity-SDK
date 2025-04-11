using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Elympics.Lobby;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using Elympics.Rooms.Models;
using Elympics.Tests.Common.RoomMocks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Elympics.Tests.Common.AsyncAsserts;

#nullable enable

namespace Elympics.Tests.Rooms
{
    [Category("Rooms")]
    public class TestRoom
    {
        private const string RegionName = "test-region";

        private static readonly Guid RoomIdForTesting = new("10100000000000000000000000000001");
        private static readonly RoomStateChanged InitialRoomState = RoomsTestUtility.PrepareInitialRoomState(RoomIdForTesting);
        private static readonly SessionConnectionDetails ConnectionDetails = new("url", new AuthData(Guid.Empty, "", ""), Guid.Empty, "", RegionName);

        [Test]
        public void TestRoomStateUpdate()
        {
            var roomState = new RoomState(InitialRoomState);
            Assert.AreEqual(roomState.Users.Count, InitialRoomState.Users.Count);
            Assert.AreEqual(roomState.Users[0], InitialRoomState.Users[0]);
        }

        [UnityTest]
        public IEnumerator TestDisposedRoom() => UniTask.ToCoroutine(async () =>
        {
            var room = new Room(null!, null!, RoomIdForTesting, InitialRoomState);

            room.Dispose();

            _ = Assert.Throws<RoomDisposedException>(() => _ = room.RoomId);
            _ = Assert.Throws<RoomDisposedException>(() => _ = room.State);
            _ = Assert.Throws<RoomDisposedException>(() => _ = room.IsJoined);
            _ = Assert.Throws<RoomDisposedException>(() => _ = room.HasMatchmakingEnabled);
            _ = Assert.Throws<RoomDisposedException>(() => _ = room.IsMatchAvailable);
            _ = Assert.Throws<RoomDisposedException>(() => ((IRoom)room).UpdateState(default!));
            _ = Assert.Throws<RoomDisposedException>(() => ((IRoom)room).UpdateState(default!, default!));
            _ = await AssertThrowsAsync<RoomDisposedException>(async () => await room.ChangeTeam(default));
            _ = await AssertThrowsAsync<RoomDisposedException>(async () => await ((IRoom)room).BecomeSpectator());
            _ = await AssertThrowsAsync<RoomDisposedException>(async () => await room.MarkYourselfReady());
            _ = await AssertThrowsAsync<RoomDisposedException>(async () => await room.MarkYourselfUnready());
            _ = await AssertThrowsAsync<RoomDisposedException>(async () => await room.StartMatchmaking());
            _ = await AssertThrowsAsync<RoomDisposedException>(async () => await room.CancelMatchmaking());
            _ = await AssertThrowsAsync<RoomDisposedException>(async () => await room.UpdateRoomParams());
            _ = Assert.Throws<RoomDisposedException>(() => room.PlayAvailableMatch());
            _ = await AssertThrowsAsync<RoomDisposedException>(async () => await room.Leave());
        });

        [UnityTest]
        public IEnumerator CallingMethodsRequiringBeingInsideTheRoomShouldThrowIfTheRoomIsNotJoined() => UniTask.ToCoroutine(async () =>
        {
            var room = new Room(null!, null!, RoomIdForTesting, InitialRoomState);

            _ = Assert.Throws<RoomNotJoinedException>(() => _ = room.IsMatchAvailable);
            _ = await AssertThrowsAsync<RoomNotJoinedException>(async () => await room.ChangeTeam(default));
            _ = await AssertThrowsAsync<RoomNotJoinedException>(async () => await ((IRoom)room).BecomeSpectator());
            _ = await AssertThrowsAsync<RoomNotJoinedException>(async () => await room.MarkYourselfReady());
            _ = await AssertThrowsAsync<RoomNotJoinedException>(async () => await room.MarkYourselfUnready());
            _ = await AssertThrowsAsync<RoomNotJoinedException>(async () => await room.StartMatchmaking());
            _ = await AssertThrowsAsync<RoomNotJoinedException>(async () => await room.CancelMatchmaking());
            _ = await AssertThrowsAsync<RoomNotJoinedException>(async () => await room.UpdateRoomParams());
            _ = Assert.Throws<RoomNotJoinedException>(() => room.PlayAvailableMatch());
            _ = await AssertThrowsAsync<RoomNotJoinedException>(async () => await room.Leave());
        });

        [UnityTest]
        public IEnumerator TestRoomWithoutPrivilegedHost() => UniTask.ToCoroutine(async () =>
        {
            var room = new Room(null!, null!, RoomIdForTesting, InitialRoomState with
            {
                HasPrivilegedHost = false,
            }, true);

            _ = await AssertThrowsAsync<RoomPrivilegeException>(async () => await room.UpdateRoomParams());
        });

        [UnityTest]
        public IEnumerator TestRoomWithoutMatchmakingFunctionality() => UniTask.ToCoroutine(async () =>
        {
            var room = new Room(null!, null!, RoomIdForTesting, InitialRoomState with
            {
                MatchmakingData = null,
            }, true);

            _ = Assert.Throws<MatchmakingException>(() => _ = room.IsMatchAvailable);
            _ = await AssertThrowsAsync<MatchmakingException>(async () => await room.ChangeTeam(default));
            _ = await AssertThrowsAsync<MatchmakingException>(async () => await ((IRoom)room).BecomeSpectator());
            _ = await AssertThrowsAsync<MatchmakingException>(async () => await room.MarkYourselfReady());
            _ = await AssertThrowsAsync<MatchmakingException>(async () => await room.MarkYourselfUnready());
            _ = await AssertThrowsAsync<MatchmakingException>(async () => await room.StartMatchmaking());
            _ = await AssertThrowsAsync<MatchmakingException>(async () => await room.CancelMatchmaking());
            _ = Assert.Throws<MatchmakingException>(() => room.PlayAvailableMatch());
        });

        [UnityTest]
        public IEnumerator TestUpdateRoomParamsNullParameters() => UniTask.ToCoroutine(async () =>
        {
            var room = new Room(null!, null!, RoomIdForTesting, InitialRoomState, true);

            await room.UpdateRoomParams();

            LogAssert.Expect(LogType.Warning, new Regex("No change compared to current room parameters."));
        });

        private readonly byte[] _keyInLimit = Enumerable.Repeat((byte)0x4b, 512).ToArray();
        private readonly byte[] _valueOverTheLimit = Enumerable.Repeat((byte)0x56, 513).ToArray();
        private readonly byte[] _valueInLimit = Enumerable.Repeat((byte)0x56, 512).ToArray();

        [UnityTest]
        public IEnumerator TestUpdateRoomParamsWithCustomRoomDataExceedingMaxSizeLimit() => UniTask.ToCoroutine(async () =>
        {
            var room = new Room(null!, null!, RoomIdForTesting, InitialRoomState, true);
            var key = Encoding.UTF8.GetString(_keyInLimit);
            var value = Encoding.UTF8.GetString(_valueOverTheLimit);
            var newCustomData = new Dictionary<string, string>
            {
                { key, value },
            };
            _ = await AssertThrowsAsync<RoomDataMemoryException>(async () => await room.UpdateRoomParams(customRoomData: newCustomData));
        });

        [UnityTest]
        public IEnumerator TestUpdateRoomParamsWithCustomRoomDataEqualToMaxLimit() => UniTask.ToCoroutine(async () =>
        {
            var room = new Room(null!, new RoomClientMock(), RoomIdForTesting, InitialRoomState, true);
            var key = Encoding.UTF8.GetString(_keyInLimit);
            var value = Encoding.UTF8.GetString(_keyInLimit);
            var newCustomData = new Dictionary<string, string>
            {
                { key, value },
            };
            await room.UpdateRoomParams(customRoomData: newCustomData);
        });

        [UnityTest]
        public IEnumerator TestUpdateRoomParamsWithCustomMatchmakingDataExceedingMaxSizeLimit() => UniTask.ToCoroutine(async () =>
        {
            IRoom room = new Room(null!, null!, RoomIdForTesting, InitialRoomState, true);
            room.UpdateState(InitialRoomState, new RoomStateDiff());
            var key = Encoding.UTF8.GetString(_keyInLimit);
            var value = Encoding.UTF8.GetString(_valueOverTheLimit);
            var newCustomData = new Dictionary<string, string>
            {
                { key, value },
            };
            _ = await AssertThrowsAsync<RoomDataMemoryException>(async () => await room.UpdateRoomParams(customMatchmakingData: newCustomData));
        });

        [UnityTest]
        public IEnumerator TestUpdateRoomParamsWithCustomMatchmakingDataEqualToMaxLimit() => UniTask.ToCoroutine(async () =>
        {
            IRoom room = new Room(null!, new RoomClientMock(), RoomIdForTesting, InitialRoomState, true);
            room.UpdateState(InitialRoomState, new RoomStateDiff());
            var key = Encoding.UTF8.GetString(_keyInLimit);
            var value = Encoding.UTF8.GetString(_valueInLimit);
            var newCustomData = new Dictionary<string, string>
            {
                { key, value },
            };
            await room.UpdateRoomParams(null, null, null, newCustomData);
        });

        [UnityTest]
        public IEnumerator TestUpdateCustomRoomParamsWithTheSameParameters() => UniTask.ToCoroutine(async () =>
        {
            const string roomName = "testRoomName";
            const bool isPrivate = true;
            var roomCustomData = new Dictionary<string, string>
            {
                { "testKey", "testValue" },
            };
            var room = new Room(null!, null!, RoomIdForTesting, InitialRoomState with
            {
                RoomName = roomName,
                IsPrivate = isPrivate,
                CustomData = roomCustomData,
            }, true);

            await room.UpdateRoomParams(roomName, isPrivate, roomCustomData);
            LogAssert.Expect(LogType.Warning, new Regex("No change compared to current room parameters."));
        });

        [UnityTest]
        public IEnumerator TestUpdateCustomMatchmakingParamsWithTheSameParameters() => UniTask.ToCoroutine(async () =>
        {
            const string roomName = "testRoomName";
            const bool isPrivate = true;
            var customMatchmakingData = new Dictionary<string, string>
            {
                { "testKey", "testValue" },
            };
            var room = new Room(null!, null!, RoomIdForTesting, InitialRoomState with
            {
                RoomName = roomName,
                IsPrivate = isPrivate,
                MatchmakingData = InitialRoomState.MatchmakingData! with
                {
                    CustomData = customMatchmakingData,
                },
            }, true);
            await room.UpdateRoomParams(roomName, isPrivate, null, customMatchmakingData);
            LogAssert.Expect(LogType.Warning, new Regex("No change compared to current room parameters."));
        });

        [Test]
        public void PlayingAvailableMatchShouldSucceedIfMatchDetailsAreAvailable()
        {
            var matchLauncher = new MatchLauncherMock { IsCurrentlyInMatch = false };
            var roomsClient = new RoomClientMock();
            roomsClient.SetSessionConnectionDetails(ConnectionDetails);

            var roomState = RoomsTestUtility.PrepareInitialRoomState(RoomIdForTesting, mmState: MatchmakingState.Playing);
            var room = new Room(matchLauncher, roomsClient, RoomIdForTesting, roomState, true);
            var matchmakingData = roomState.MatchmakingData!;
            var matchData = matchmakingData.MatchData!;

            // Act
            room.PlayAvailableMatch();

            Assert.That(matchLauncher.PlayMatchCalledArgs, Is.Not.Null);
            Assert.That(matchLauncher.PlayMatchCalledArgs, Is.EqualTo(new MatchmakingFinishedData(matchData.MatchId, matchData.MatchDetails!, matchmakingData.QueueName, RegionName)));
        }

        [Test]
        public void PlayingAvailableMatchShouldFailIfMatchStateIsNotRunning()
        {
            var matchLauncher = new MatchLauncherMock { IsCurrentlyInMatch = false };
            var roomsClient = new RoomClientMock();
            roomsClient.SetSessionConnectionDetails(ConnectionDetails);

            var roomState = RoomsTestUtility.PrepareInitialRoomState(RoomIdForTesting, mmState: MatchmakingState.Playing);
            var roomStateWithoutMatchDetails = roomState with
            {
                MatchmakingData = roomState.MatchmakingData! with
                {
                    MatchData = roomState.MatchmakingData.MatchData! with
                    {
                        State = MatchState.Initializing,
                    },
                },
            };
            var room = new Room(matchLauncher, roomsClient, RoomIdForTesting, roomStateWithoutMatchDetails, true);

            // Act
            TestDelegate testDelegate = () => room.PlayAvailableMatch();

            Assert.That(testDelegate, Throws.InvalidOperationException.With.Message.Contain("Running"));
            Assert.That(matchLauncher.PlayMatchCalledArgs, Is.Null);
        }

        [Test]
        public void PlayingAvailableMatchShouldFailIfMatchDetailsAreNotAvailable()
        {
            var matchLauncher = new MatchLauncherMock { IsCurrentlyInMatch = false };
            var roomsClient = new RoomClientMock();
            roomsClient.SetSessionConnectionDetails(ConnectionDetails);

            var roomState = RoomsTestUtility.PrepareInitialRoomState(RoomIdForTesting, mmState: MatchmakingState.Playing);
            var roomStateWithoutMatchDetails = roomState with
            {
                MatchmakingData = roomState.MatchmakingData! with
                {
                    MatchData = roomState.MatchmakingData.MatchData! with
                    {
                        State = MatchState.Running,
                        MatchDetails = null,
                    },
                },
            };
            var room = new Room(matchLauncher, roomsClient, RoomIdForTesting, roomStateWithoutMatchDetails, true);

            // Act
            TestDelegate testDelegate = () => room.PlayAvailableMatch();

            Assert.That(testDelegate, Throws.InvalidOperationException.With.Message.Contain("match details"));
            Assert.That(matchLauncher.PlayMatchCalledArgs, Is.Null);
        }

        [Test]
        public void PlayingAvailableMatchShouldFailIfMatchmakingStateIsNotPlaying()
        {
            var matchLauncher = new MatchLauncherMock { IsCurrentlyInMatch = false };
            var roomsClient = new RoomClientMock();
            roomsClient.SetSessionConnectionDetails(ConnectionDetails);

            var roomState = RoomsTestUtility.PrepareInitialRoomState(RoomIdForTesting, mmState: MatchmakingState.Playing) with
            {
                MatchmakingData = InitialRoomState.MatchmakingData! with
                {
                    State = MatchmakingState.Matchmaking,
                },
            };
            var room = new Room(matchLauncher, roomsClient, RoomIdForTesting, roomState, true);

            // Act
            TestDelegate testDelegate = () => room.PlayAvailableMatch();

            Assert.That(testDelegate, Throws.InvalidOperationException.With.Message.Contain("Playing"));
            Assert.That(matchLauncher.PlayMatchCalledArgs, Is.Null);
        }

        [Test]
        public void PlayingAvailableMatchShouldFailIfMatchDataIsNotAvailable()
        {
            var matchLauncher = new MatchLauncherMock { IsCurrentlyInMatch = false };
            var roomsClient = new RoomClientMock();
            roomsClient.SetSessionConnectionDetails(ConnectionDetails);

            var roomState = RoomsTestUtility.PrepareInitialRoomState(RoomIdForTesting, mmState: MatchmakingState.Playing) with
            {
                MatchmakingData = InitialRoomState.MatchmakingData! with
                {
                    State = MatchmakingState.Playing,
                    MatchData = null,
                },
            };
            var room = new Room(matchLauncher, roomsClient, RoomIdForTesting, roomState, true);

            // Act
            TestDelegate testDelegate = () => room.PlayAvailableMatch();

            Assert.That(testDelegate, Throws.InvalidOperationException.With.Message.Contain("match data"));
            Assert.That(matchLauncher.PlayMatchCalledArgs, Is.Null);
        }

        [Test]
        public void PlayingAvailableMatchShouldFailForNonMatchmakingRoom()
        {
            var matchLauncher = new MatchLauncherMock { IsCurrentlyInMatch = false };
            var roomsClient = new RoomClientMock();
            roomsClient.SetSessionConnectionDetails(ConnectionDetails);

            var roomState = InitialRoomState with
            {
                MatchmakingData = null,
            };
            var room = new Room(matchLauncher, roomsClient, RoomIdForTesting, roomState, true);

            // Act
            TestDelegate testDelegate = () => room.PlayAvailableMatch();

            Assert.That(testDelegate, Throws.Exception.TypeOf<MatchmakingException>().With.Message.Contain("non-matchmaking"));
            Assert.That(matchLauncher.PlayMatchCalledArgs, Is.Null);
        }
    }
}
