using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Elympics.Rooms.Models;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.TestTools;
using static Elympics.Tests.Common.AsyncAsserts;

#nullable enable

namespace Elympics.Tests.Rooms
{
    [Category("Rooms")]
    internal class TestRoomsManager_JoiningAndTracking : TestRoomsManager
    {
        private static readonly PublicRoomState InitialPublicState = Defaults.CreatePublicRoomState(RoomId, HostId);
        private static readonly RoomListChanged InitialRoomList = new(new List<ListedRoomChange>
        {
            new(RoomId, InitialPublicState),
        });

        [Test]
        public void RoomShouldBeSetAsJoinedAfterStateTrackingMessageIsReceived()
        {
            Assert.AreEqual(0, RoomsManager.ListJoinedRooms().Count);

            // Act
            EmitRoomUpdate(InitialRoomState);

            Assert.That(RoomsManager.CurrentRoom?.RoomId, Is.EqualTo(RoomId));
            var joinedRooms = RoomsManager.ListJoinedRooms();
            Assert.AreEqual(1, joinedRooms.Count);
            Assert.That(joinedRooms.Count, Is.EqualTo(1));
            Assert.That(joinedRooms[0].RoomId, Is.EqualTo(RoomId));
        }

        [Test]
        public void StateTrackingMessageShouldCauseBothJoinAndUpdateEventsToBeEmitted()
        {
            EventRegister.ListenForEvents(nameof(RoomsManager.JoinedRoom), nameof(IRoomsManager.JoinedRoomUpdated));

            // Act
            EmitRoomUpdate(InitialRoomState);

            EventRegister.AssertIfInvoked();
        }

        [UnityTest]
        public IEnumerator JoiningByIdShouldSucceedIfThereIsNoOtherRoomCurrentlyJoined() => UniTask.ToCoroutine(async () =>
        {
            SetRoomAsTrackedWhenItGetsJoined();

            RoomsClientMock.ReturnsForJoinOrCreate(() => GetResponseDelay().ContinueWith(() => RoomId));

            // Act
            var joinedRoom = await RoomsManager.JoinRoom(RoomId, null);

            Assert.That(joinedRoom.RoomId, Is.EqualTo(RoomId));
            Assert.That(joinedRoom.RoomId, Is.EqualTo(RoomsManager.CurrentRoom?.RoomId));
        });

        [UnityTest]
        public IEnumerator JoiningRoomThatIsAlreadyBeingJoinedWithRoomIdShouldResultInException() => UniTask.ToCoroutine(async () =>
        {
            await EnsureRoomIsBeingJoined(RoomId);

            // Act
            var exception = await AssertThrowsAsync<RoomAlreadyJoinedException>(async () => await RoomsManager.JoinRoom(RoomId, null));

            Assert.That(exception.RoomId, Is.EqualTo(RoomId));
            Assert.That(exception.JoinCode, Is.Null);
            Assert.That(exception.InProgress, Is.True);
        });

        [UnityTest]
        public IEnumerator JoiningRoomThatIsAlreadyBeingJoinedWithJoinCodeShouldResultInException() => UniTask.ToCoroutine(async () =>
        {
            const string testJoinCode = "test-join-code";

            await EnsureRoomIsBeingJoined(joinCode: testJoinCode);

            // Act
            var exception = await AssertThrowsAsync<RoomAlreadyJoinedException>(async () => await RoomsManager.JoinRoom(null, testJoinCode));

            Assert.That(exception.RoomId.HasValue, Is.False);
            Assert.That(exception.JoinCode, Is.EqualTo(testJoinCode));
            Assert.That(exception.InProgress, Is.True);
        });

        [Test]
        public void JoiningListedRoomShouldUpdateItsData_Users()
        {
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChanged>>(InitialRoomList);

            var joiningPlayer = Defaults.CreateUserInfo(Guid.NewGuid());
            var joinedRoomState = InitialRoomState
                .WithUserAdded(joiningPlayer);

            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.JoinedRoom));

            // Act
            EmitRoomUpdate(joinedRoomState);

            // Assert
            EventRegister.AssertIfInvoked();
            Assert.That(RoomsManager.CurrentRoom, Is.Not.Null);
            var joinedRoom = RoomsManager.CurrentRoom!;
            Assert.That(joinedRoom.State.Users.Count, Is.EqualTo(2));
            Assert.That(joinedRoom.State.Users[0].UserId, Is.EqualTo(HostId));
            Assert.That(joinedRoom.State.Users[1].UserId, Is.EqualTo(joiningPlayer.UserId));
        }

        [Test]
        public void RoomListUpdatedEventShouldBeInvokedAfterReceivingUpdatedRoomList()
        {
            EventRegister.ListenForEvents(nameof(IRoomsManager.RoomListUpdated));

            // Act
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChanged>>(InitialRoomList);

            EventRegister.AssertIfInvoked();
        }

        [Test]
        public void AvailableRoomListShouldBeExpandedCorrectlyWhenRoomListUpdateIsReceived()
        {
            Assert.That(RoomsManager.ListAvailableRooms().Count, Is.EqualTo(0));

            // Act
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChanged>>(InitialRoomList);

            Assert.That(RoomsManager.ListAvailableRooms().Count, Is.EqualTo(1));
            var availableRoom = RoomsManager.ListAvailableRooms()[0];
            Assert.That(availableRoom.RoomId, Is.EqualTo(RoomId));
            Assert.That(availableRoom.State.RoomName, Is.EqualTo(InitialPublicState.RoomName));
        }

        [Test]
        public void AvailableRoomListShouldBeReducedCorrectlyWhenRoomListUpdateIsReceived()
        {
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChanged>>(InitialRoomList);
            Assert.That(RoomsManager.ListAvailableRooms().Count, Is.EqualTo(1));
            var roomListChanged = new RoomListChanged(new List<ListedRoomChange>
            {
                new(RoomId, null),
            });

            // Act
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChanged>>(roomListChanged);

            Assert.That(RoomsManager.ListAvailableRooms().Count, Is.EqualTo(0));
        }

        private static List<(PublicRoomState ModifiedState, string[] ExpectedEvents)> availableRoomListUpdateTestCases = new()
        {
            (InitialPublicState with { RoomName = "test room name modified" }, new[] { nameof(Elympics.RoomsManager.RoomListUpdated) }),
            (InitialPublicState with { Users = new[] { Defaults.CreateUserInfo(Guid.NewGuid()), Defaults.CreateUserInfo(Guid.NewGuid()) } }, new[] { nameof(Elympics.RoomsManager.RoomListUpdated) }),
            (InitialPublicState with { IsPrivate = true }, new[] { nameof(Elympics.RoomsManager.RoomListUpdated) }),
            (InitialPublicState with { CustomData = new Dictionary<string, string> { { "test key", "test value" } } }, new[] { nameof(Elympics.RoomsManager.RoomListUpdated) }),
            (InitialPublicState with { HasPrivilegedHost = false }, new[] { nameof(Elympics.RoomsManager.RoomListUpdated) }),
            (InitialPublicState with { MatchmakingData = Defaults.CreatePublicMatchmakingData(MatchmakingState.Matchmaking) }, new[] { nameof(Elympics.RoomsManager.RoomListUpdated) }),
        };

        [Test]
        public void AvailableRoomListShouldBeModifiedCorrectlyWhenRoomListUpdateIsReceived(
            [ValueSource(nameof(availableRoomListUpdateTestCases))]
            (PublicRoomState ModifiedState, string[] ExpectedEvents) testCase)
        {
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChanged>>(InitialRoomList);

            var modifiedState = testCase.ModifiedState with { LastUpdate = Timer++ };
            var roomListChanged = new RoomListChanged(new List<ListedRoomChange>
            {
                new(RoomId, modifiedState),
            });

            EventRegister.ListenForEvents(testCase.ExpectedEvents);

            // Act
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChanged>>(roomListChanged);

            EventRegister.AssertIfInvoked();
            Assert.That(RoomsManager.ListAvailableRooms().Count, Is.EqualTo(1));
            var actualData = RoomsManager.ListAvailableRooms()[0]!;
            Assert.That(actualData.RoomId, Is.EqualTo(RoomId));
            Assert.That(actualData.State.RoomName, Is.EqualTo(modifiedState.RoomName));
            Assert.That(actualData.State.Users, Is.EqualTo(modifiedState.Users));
            Assert.That(actualData.State.IsPrivate, Is.EqualTo(modifiedState.IsPrivate));
            Assert.That(actualData.State.CustomData, Is.EqualTo(modifiedState.CustomData));
            Assert.That(actualData.State.PrivilegedHost, Is.EqualTo(modifiedState.HasPrivilegedHost));
            if (modifiedState.MatchmakingData is null)
            {
                Assert.That(actualData.State.MatchmakingData, Is.Null);
                return;
            }
            Assert.That(actualData.State.MatchmakingData, Is.Not.Null);
            var actualMatchmakingData = actualData.State.MatchmakingData!;
            Assert.That(actualMatchmakingData.MatchmakingState, Is.EqualTo(modifiedState.MatchmakingData.State));
            Assert.That(actualMatchmakingData.QueueName, Is.EqualTo(modifiedState.MatchmakingData.QueueName));
            Assert.That(actualMatchmakingData.TeamCount, Is.EqualTo(modifiedState.MatchmakingData.TeamCount));
            Assert.That(actualMatchmakingData.TeamSize, Is.EqualTo(modifiedState.MatchmakingData.TeamSize));
            Assert.That(actualMatchmakingData.CustomData, Is.EqualTo(modifiedState.MatchmakingData.CustomData));
            Assert.That(actualMatchmakingData.BetDetails, Is.EqualTo(modifiedState.MatchmakingData.BetDetails));
        }

        private static List<(RoomStateChanged ModifiedState, string[] ExpectedEvents)> roomUpdateTestCases = new()
        {
            (
                InitialRoomState.WithNameChanged("New Room Name"),
                new[]
                {
                    nameof(Elympics.RoomsManager.JoinedRoomUpdated),
                    nameof(Elympics.RoomsManager.RoomNameChanged),
                }
            ),
            (
                InitialRoomState
                    .WithUserAdded(Defaults.CreateUserInfo())
                    .WithUserAdded(Defaults.CreateUserInfo())
                    .WithUserRemoved(HostId),
                new[]
                {
                    nameof(Elympics.RoomsManager.JoinedRoomUpdated),
                    nameof(Elympics.RoomsManager.UserLeft),
                    nameof(Elympics.RoomsManager.UserJoined),
                    nameof(Elympics.RoomsManager.UserJoined),
                    nameof(Elympics.RoomsManager.UserCountChanged),
                    nameof(Elympics.RoomsManager.HostChanged),
                }
            ),
            (
                InitialRoomState.WithPublicnessChanged(true),
                new[]
                {
                    nameof(Elympics.RoomsManager.JoinedRoomUpdated),
                    nameof(Elympics.RoomsManager.RoomPublicnessChanged),
                }
            ),
            (
                InitialRoomState.WithCustomDataAdded("test key", "test value"),
                new[]
                {
                    nameof(Elympics.RoomsManager.JoinedRoomUpdated),
                    nameof(Elympics.RoomsManager.CustomRoomDataChanged),
                }
            ),
            (
                InitialRoomState.WithNoPrivilegedHost(),
                new[]
                {
                    nameof(Elympics.RoomsManager.JoinedRoomUpdated),
                }
            ),
            (
                InitialRoomState.WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingState.Matchmaking)),
                new[]
                {
                    nameof(Elympics.RoomsManager.JoinedRoomUpdated),
                    nameof(Elympics.RoomsManager.MatchmakingDataChanged),
                    nameof(Elympics.RoomsManager.MatchmakingStarted),
                }
            ),
            (
                InitialRoomState.WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingState.Playing)),
                new[]
                {
                    nameof(Elympics.RoomsManager.JoinedRoomUpdated),
                    nameof(Elympics.RoomsManager.MatchmakingDataChanged),
                    nameof(Elympics.RoomsManager.MatchDataReceived),
                }
            ),
            (
                InitialRoomState,
                new[]
                {
                    nameof(Elympics.RoomsManager.JoinedRoomUpdated),
                }
            ),
        };

        [Test]
        public void AvailableRoomListShouldBeModifiedCorrectlyWhenRoomUpdateIsReceived(
            [ValueSource(nameof(roomUpdateTestCases))]
            (RoomStateChanged ModifiedState, string[] ExpectedEvents) testCase)
        {
            EmitRoomUpdate(InitialRoomState);

            Assert.That(RoomsManager.CurrentRoom, Is.Not.Null);

            var modifiedState = testCase.ModifiedState;

            EventRegister.ListenForEvents(testCase.ExpectedEvents);

            // Act
            EmitRoomUpdate(modifiedState);

            EventRegister.AssertIfInvoked();
            Assert.That(RoomsManager.CurrentRoom, Is.Not.Null);
            var actualData = RoomsManager.CurrentRoom!;
            Assert.That(actualData.RoomId, Is.EqualTo(RoomId));
            Assert.That(actualData.State.RoomName, Is.EqualTo(modifiedState.RoomName));
            Assert.That(actualData.State.JoinCode, Is.EqualTo(modifiedState.JoinCode));
            Assert.That(actualData.State.Users, Is.EqualTo(modifiedState.Users));
            Assert.That(actualData.State.IsPrivate, Is.EqualTo(modifiedState.IsPrivate));
            Assert.That(actualData.State.CustomData, Is.EqualTo(modifiedState.CustomData));
            Assert.That(actualData.State.PrivilegedHost, Is.EqualTo(modifiedState.HasPrivilegedHost));
            if (modifiedState.MatchmakingData is null)
            {
                Assert.That(actualData.State.MatchmakingData, Is.Null);
                return;
            }
            Assert.That(actualData.State.MatchmakingData, Is.Not.Null);
            var actualMmData = actualData.State.MatchmakingData!;
            Assert.That(actualMmData.MatchmakingState, Is.EqualTo(modifiedState.MatchmakingData.State));
            Assert.That(actualMmData.QueueName, Is.EqualTo(modifiedState.MatchmakingData.QueueName));
            Assert.That(actualMmData.TeamSize, Is.EqualTo(modifiedState.MatchmakingData.TeamSize));
            Assert.That(actualMmData.TeamCount, Is.EqualTo(modifiedState.MatchmakingData.TeamCount));
            Assert.That(actualMmData.MatchData, Is.EqualTo(modifiedState.MatchmakingData.MatchData));
            Assert.That(actualMmData.CustomData, Is.EqualTo(modifiedState.MatchmakingData.CustomData));
            Assert.That(actualMmData.BetDetails, Is.EqualTo(modifiedState.MatchmakingData.BetDetails));
        }

        [Test]
        public void RoomUpdateShouldInvokeRoomUpdatedEvent()
        {
            EmitRoomUpdate(InitialRoomState);
            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated));

            // Act
            EmitRoomUpdate(InitialRoomState);

            EventRegister.AssertIfInvoked();
        }

        [Test]
        public void RoomUpdateShouldNotDuplicateRooms()
        {
            EmitRoomUpdate(InitialRoomState);
            Assert.AreEqual(1, RoomsManager.ListJoinedRooms().Count);
            Assert.AreEqual(RoomId, RoomsManager.ListJoinedRooms()[0].RoomId);

            // Act
            EmitRoomUpdate(InitialRoomState);

            Assert.AreEqual(1, RoomsManager.ListJoinedRooms().Count);
            Assert.AreEqual(RoomId, RoomsManager.ListJoinedRooms()[0].RoomId);
        }

        [Test]
        public void LateRoomUpdateShouldBeDiscarded()
        {
            var lateRoomState = InitialRoomState.WithLastUpdate(Timer++);
            var roomState = InitialRoomState.WithLastUpdate(Timer++);
            EmitRoomUpdate(roomState, false);
            EventRegister.Reset();

            // Act
            EmitRoomUpdate(lateRoomState, false);

            EventRegister.AssertIfInvoked();
        }

        [Test]
        public void TestUserJoinedInvoked()
        {
            EmitRoomUpdate(InitialRoomState);

            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.UserJoined), nameof(IRoomsManager.UserCountChanged));
            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                Users = InitialRoomState.Users.Append(new UserInfo(Guid.NewGuid(), 0, false, string.Empty, null)).ToList(),
            };
            EmitRoomUpdate(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
        }

        [Test]
        public void TestUserLeftInvoked()
        {
            var matchmakingRoomState = InitialRoomState with
            {
                Users = InitialRoomState.Users.Append(new UserInfo(Guid.NewGuid(), 0, false, string.Empty, null)).ToList(),
            };
            EmitRoomUpdate(matchmakingRoomState);

            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.UserLeft), nameof(IRoomsManager.UserCountChanged));
            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                Users = matchmakingRoomState.Users.Take(matchmakingRoomState.Users.Count - 1).ToList(),
            };
            EmitRoomUpdate(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
        }

        [Test]
        public void TestHostChangedInvoked()
        {
            var matchmakingRoomState = InitialRoomState with
            {
                Users = InitialRoomState.Users.Append(new UserInfo(Guid.NewGuid(), 0, false, string.Empty, null)).ToList(),
            };
            EmitRoomUpdate(matchmakingRoomState);
            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.HostChanged));
            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                Users = matchmakingRoomState.Users.Reverse().ToList(),
            };
            var newHost = matchmakingRoomState.Users[0].UserId;
            EmitRoomUpdate(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.AreEqual(newHost, RoomsManager.ListJoinedRooms()[0].State.Host.UserId);
        }

        [Test]
        public void TestUserReadinessChangedToReadyInvoked()
        {
            EmitRoomUpdate(InitialRoomState);

            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.UserReadinessChanged));
            var readyUser = InitialRoomState.Users[0] with
            {
                IsReady = true,
            };
            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                Users = InitialRoomState.Users.Skip(1).Prepend(readyUser).ToList(),
            };
            EmitRoomUpdate(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.IsTrue(RoomsManager.ListJoinedRooms()[0].State.Users[0].IsReady);
        }

        [Test]
        public void TestUserReadinessChangedToUnreadyInvoked()
        {
            var readyUser = InitialRoomState.Users[0] with
            {
                IsReady = true,
            };
            var matchmakingRoomState = InitialRoomState with
            {
                Users = InitialRoomState.Users.Skip(1).Prepend(readyUser).ToList(),
            };
            EmitRoomUpdate(matchmakingRoomState);

            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.UserReadinessChanged));
            var unreadyUser = matchmakingRoomState.Users[0] with
            {
                IsReady = false,
            };
            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                Users = matchmakingRoomState.Users.Skip(1).Prepend(unreadyUser).ToList(),
            };
            EmitRoomUpdate(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.IsFalse(RoomsManager.ListJoinedRooms()[0].State.Users[0].IsReady);
        }

        [Test]
        public void TestTeamChangedInvoked()
        {
            EmitRoomUpdate(InitialRoomState);

            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.UserChangedTeam));
            var userWithChangedTeam = InitialRoomState.Users[0] with
            {
                TeamIndex = 1,
            };
            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                Users = InitialRoomState.Users.Skip(1).Prepend(userWithChangedTeam).ToList(),
            };
            EmitRoomUpdate(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.AreEqual(1, RoomsManager.ListJoinedRooms()[0].State.Users[0].TeamIndex);
        }

        [Test]
        public void TestRoomLeftInvoked()
        {
            EmitRoomUpdate(InitialRoomState);
            EventRegister.ListenForEvents(nameof(IRoomsManager.LeftRoom));
            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
            };
            var leaveRoomArgs = new LeftRoomArgs(matchmakingRoomState.RoomId, LeavingReason.RoomClosed);
            RoomsClientMock.LeftRoom += Raise.Event<Action<LeftRoomArgs>>(leaveRoomArgs);
            EventRegister.AssertIfInvoked();
        }

        [Test]
        public void TestRoomAfterLeftRoom()
        {
            EmitRoomUpdate(InitialRoomState);
            var leaveRoomArgs = new LeftRoomArgs(InitialRoomState.RoomId, LeavingReason.RoomClosed);
            RoomsClientMock.LeftRoom += Raise.Event<Action<LeftRoomArgs>>(leaveRoomArgs);
            Assert.AreEqual(0, RoomsManager.ListJoinedRooms().Count);
        }

        [Test]
        public void TestRoomNameChangedInvoked()
        {
            const string newRoomName = "New Room Name";
            EmitRoomUpdate(InitialRoomState);
            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                RoomName = newRoomName,
            };
            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.RoomNameChanged));
            EmitRoomUpdate(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.AreSame(newRoomName, RoomsManager.ListJoinedRooms()[0].State.RoomName);
        }

        [Test]
        public void TestIsPrivateChangedInvoked()
        {
            EmitRoomUpdate(InitialRoomState);
            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                IsPrivate = true,
            };
            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.RoomPublicnessChanged));

            // Act
            EmitRoomUpdate(matchmakingRoomState);

            EventRegister.AssertIfInvoked();
            Assert.AreEqual(true, RoomsManager.ListJoinedRooms()[0].State.IsPrivate);
        }

        private UniTask EnsureRoomIsBeingJoined(Guid? roomId = null, string? joinCode = null)
        {
            SetRoomAsTrackedWhenItGetsJoined();

            var joinMethodCalled = false;

            RoomsClientMock.ReturnsForJoinOrCreate(() => GetEternalDelay().ContinueWith(() => RoomId), _ => joinMethodCalled = true);

            RoomsManager.JoinRoom(roomId, joinCode).Forget(_ => { });

            return UniTask.WaitUntil(() => joinMethodCalled);
        }
    }
}
