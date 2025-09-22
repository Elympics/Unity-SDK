using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Rooms.InternalModels;
using Elympics.Communication.Rooms.InternalModels.FromRooms;
using Elympics.Communication.Authentication.Models;
using Elympics.Communication.Authentication.Models.Internal;
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
        private static readonly RoomListChangedDto InitialRoomList = new(new List<ListedRoomChange>
        {
            new(RoomId, InitialPublicState),
        });

        [Test]
        public void RoomShouldBeSetAsJoinedAfterStateTrackingMessageIsReceived()
        {
            Assert.IsNull(RoomsManager.CurrentRoom);

            // Act
            EmitRoomUpdate(InitialRoomState);

            Assert.That(RoomsManager.CurrentRoom?.RoomId, Is.EqualTo(RoomId));
            var joinedRoom = RoomsManager.CurrentRoom;
            Assert.NotNull(joinedRoom);
            Assert.That(joinedRoom!.RoomId, Is.EqualTo(RoomId));
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
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChangedDto>>(InitialRoomList);

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
            Assert.That(joinedRoom.State.Users[0].User.UserId, Is.EqualTo(HostId));
            Assert.That(joinedRoom.State.Users[1].User.UserId, Is.EqualTo(Guid.Parse(joiningPlayer.User.userId)));
        }

        [Test]
        public void RoomListUpdatedEventShouldBeInvokedAfterReceivingUpdatedRoomList()
        {
            EventRegister.ListenForEvents(nameof(IRoomsManager.RoomListUpdated));

            // Act
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChangedDto>>(InitialRoomList);

            EventRegister.AssertIfInvoked();
        }

        [Test]
        public void AvailableRoomListShouldBeExpandedCorrectlyWhenRoomListUpdateIsReceived()
        {
            Assert.That(RoomsManager.ListAvailableRooms().Count, Is.EqualTo(0));

            // Act
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChangedDto>>(InitialRoomList);

            Assert.That(RoomsManager.ListAvailableRooms().Count, Is.EqualTo(1));
            var availableRoom = RoomsManager.ListAvailableRooms()[0];
            Assert.That(availableRoom.RoomId, Is.EqualTo(RoomId));
            Assert.That(availableRoom.State.RoomName, Is.EqualTo(InitialPublicState.RoomName));
        }

        [Test]
        public void AvailableRoomListShouldBeReducedCorrectlyWhenRoomListUpdateIsReceived()
        {
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChangedDto>>(InitialRoomList);
            Assert.That(RoomsManager.ListAvailableRooms().Count, Is.EqualTo(1));
            var roomListChanged = new RoomListChangedDto(new List<ListedRoomChange>
            {
                new(RoomId, null),
            });

            // Act
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChangedDto>>(roomListChanged);

            Assert.That(RoomsManager.ListAvailableRooms().Count, Is.EqualTo(0));
        }

        private static List<(PublicRoomState ModifiedState, string[] ExpectedEvents)> availableRoomListUpdateTestCases = new()
        {
            (InitialPublicState with { RoomName = "test room name modified" }, new[] { nameof(Elympics.RoomsManager.RoomListUpdated) }),
            (InitialPublicState with { Users = new[] { Defaults.CreateUserInfo(Guid.NewGuid()), Defaults.CreateUserInfo(Guid.NewGuid()) } }, new[] { nameof(Elympics.RoomsManager.RoomListUpdated) }),
            (InitialPublicState with { IsPrivate = true }, new[] { nameof(Elympics.RoomsManager.RoomListUpdated) }),
            (InitialPublicState with { CustomData = new Dictionary<string, string> { { "test key", "test value" } } }, new[] { nameof(Elympics.RoomsManager.RoomListUpdated) }),
            (InitialPublicState with { HasPrivilegedHost = false }, new[] { nameof(Elympics.RoomsManager.RoomListUpdated) }),
            (InitialPublicState with { MatchmakingData = Defaults.CreatePublicMatchmakingData(MatchmakingStateDto.Matchmaking) }, new[] { nameof(Elympics.RoomsManager.RoomListUpdated) }),
        };

        [Test]
        public void AvailableRoomListShouldBeModifiedCorrectlyWhenRoomListUpdateIsReceived(
            [ValueSource(nameof(availableRoomListUpdateTestCases))]
            (PublicRoomState ModifiedState, string[] ExpectedEvents) testCase)
        {
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChangedDto>>(InitialRoomList);

            var modifiedState = testCase.ModifiedState with { LastUpdate = Timer++ };
            var roomListChanged = new RoomListChangedDto(new List<ListedRoomChange>
            {
                new(RoomId, modifiedState),
            });

            EventRegister.ListenForEvents(testCase.ExpectedEvents);

            // Act
            RoomsClientMock.RoomListChanged += Raise.Event<Action<RoomListChangedDto>>(roomListChanged);

            EventRegister.AssertIfInvoked();
            Assert.That(RoomsManager.ListAvailableRooms().Count, Is.EqualTo(1));
            var actualData = RoomsManager.ListAvailableRooms()[0]!;
            Assert.That(actualData.RoomId, Is.EqualTo(RoomId));
            Assert.That(actualData.State.RoomName, Is.EqualTo(modifiedState.RoomName));
            Assert.That(actualData.State.Users, Is.EqualTo(modifiedState.Users.Select(RoomsMapper.Map)));
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
            Assert.That(actualMatchmakingData.MatchmakingState, Is.EqualTo(modifiedState.MatchmakingData.State.Map()));
            Assert.That(actualMatchmakingData.QueueName, Is.EqualTo(modifiedState.MatchmakingData.QueueName));
            Assert.That(actualMatchmakingData.TeamCount, Is.EqualTo(modifiedState.MatchmakingData.TeamCount));
            Assert.That(actualMatchmakingData.TeamSize, Is.EqualTo(modifiedState.MatchmakingData.TeamSize));
            Assert.That(actualMatchmakingData.CustomData, Is.EqualTo(modifiedState.MatchmakingData.CustomData));
            Assert.That(actualMatchmakingData.BetDetails, Is.EqualTo(modifiedState.MatchmakingData.BetDetails?.Map()));
        }

        private static List<(RoomStateChangedDto ModifiedState, string[] ExpectedEvents)> roomUpdateTestCases = new()
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
                InitialRoomState.WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingStateDto.Matchmaking)),
                new[]
                {
                    nameof(Elympics.RoomsManager.JoinedRoomUpdated),
                    nameof(Elympics.RoomsManager.MatchmakingDataChanged),
                    nameof(Elympics.RoomsManager.MatchmakingStarted),
                }
            ),
            (
                InitialRoomState.WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingStateDto.Playing)),
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
            (RoomStateChangedDto ModifiedState, string[] ExpectedEvents) testCase)
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
            Assert.That(actualData.State.Users, Is.EqualTo(modifiedState.Users.Select(RoomsMapper.Map)));
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
            Assert.That(actualMmData.MatchmakingState, Is.EqualTo(modifiedState.MatchmakingData.State.Map()));
            Assert.That(actualMmData.QueueName, Is.EqualTo(modifiedState.MatchmakingData.QueueName));
            Assert.That(actualMmData.TeamSize, Is.EqualTo(modifiedState.MatchmakingData.TeamSize));
            Assert.That(actualMmData.TeamCount, Is.EqualTo(modifiedState.MatchmakingData.TeamCount));
            Assert.That(actualMmData.MatchData, Is.EqualTo(modifiedState.MatchmakingData.MatchData?.Map()));
            Assert.That(actualMmData.CustomData, Is.EqualTo(modifiedState.MatchmakingData.CustomData));
            Assert.That(actualMmData.BetDetails, Is.EqualTo(modifiedState.MatchmakingData.BetDetails?.Map()));
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
            Assert.NotNull(RoomsManager.CurrentRoom);
            Assert.AreEqual(RoomId, RoomsManager.CurrentRoom!.RoomId);

            // Act
            EmitRoomUpdate(InitialRoomState);

            Assert.NotNull(RoomsManager.CurrentRoom);
            Assert.AreEqual(RoomId, RoomsManager.CurrentRoom!.RoomId);
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
                Users = InitialRoomState.Users.Append(new UserInfoDto(0, false, new Dictionary<string, string>(), new ElympicsUserDTO(Guid.NewGuid().ToString(), "", (int)NicknameType.Common, ""))).ToList(),
            };
            EmitRoomUpdate(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
        }

        [Test]
        public void TestUserLeftInvoked()
        {
            var matchmakingRoomState = InitialRoomState with
            {
                Users = InitialRoomState.Users.Append(new UserInfoDto(0, false, new Dictionary<string, string>(), new ElympicsUserDTO(Guid.NewGuid().ToString(), "", (int)NicknameType.Common, ""))).ToList(),            };
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
                Users = InitialRoomState.Users.Append(new UserInfoDto(0, false, new Dictionary<string, string>(), new ElympicsUserDTO(Guid.NewGuid().ToString(), "", (int)NicknameType.Common, ""))).ToList(),

            };
            EmitRoomUpdate(matchmakingRoomState);
            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.HostChanged));
            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                Users = matchmakingRoomState.Users.Reverse().ToList(),
            };
            var newHost = Guid.Parse(matchmakingRoomState.Users[0].User.userId);
            EmitRoomUpdate(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.AreEqual(newHost, RoomsManager.CurrentRoom.State.Host.User.UserId);
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
            Assert.IsTrue(RoomsManager.CurrentRoom.State.Users[0].IsReady);
        }

        [Test]
        public void TestCustomPlayerDataChangedInvoked()
        {
            const string newKey = "newKey";
            const string newValue = "newValue";

            EmitRoomUpdate(InitialRoomState);

            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.CustomPlayerDataChanged));
            var user = InitialRoomState.Users[0] with
            {
                CustomPlayerData = new Dictionary<string, string> { { newKey, newValue } }
            };
            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                Users = InitialRoomState.Users.Skip(1).Prepend(user).ToList(),
            };
            EmitRoomUpdate(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.IsTrue(RoomsManager.CurrentRoom!.State.Users[0].CustomPlayerData.ContainsKey(newKey));
            Assert.AreEqual(RoomsManager.CurrentRoom!.State.Users[0].CustomPlayerData[newKey], newValue);
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
            Assert.IsFalse(RoomsManager.CurrentRoom.State.Users[0].IsReady);
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
            Assert.AreEqual(1, RoomsManager.CurrentRoom.State.Users[0].TeamIndex);
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
            Assert.Null(RoomsManager.CurrentRoom);
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
            Assert.AreSame(newRoomName, RoomsManager.CurrentRoom.State.RoomName);
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
            Assert.AreEqual(true, RoomsManager.CurrentRoom.State.IsPrivate);
        }

        private class DummyException : Exception
        {
            public DummyException(string message)
                : base(message)
            { }
        }

        [UnityTest]
        public IEnumerator ExceptionThrownByRoomsClientShouldBeForwarded() => UniTask.ToCoroutine(async () =>
        {
            var expected = new DummyException("My test exception");

            _ = RoomsClientMock.JoinRoom("", null)
                .ReturnsForAnyArgs(UniTask.FromException<Guid>(expected));

            // Act
            var exception = await AssertThrowsAsync<DummyException>(async () => await RoomsManager.JoinRoom(null, ""));

            Assert.That(exception.Message, Is.EqualTo(expected.Message));
        });

        [UnityTest]
        public IEnumerator ExceptionFromJoiningOperationShouldResetRoomJoiningState() => UniTask.ToCoroutine(async () =>
        {
            var expected = new DummyException("My test exception");

            _ = RoomsClientMock.JoinRoom("", null)
                .ReturnsForAnyArgs(UniTask.FromException<Guid>(expected));

            RoomJoiningState? lastRoomJoiningState = null;
            RoomJoiner.JoiningStateChanged += UpdateLastJoiningState;

            // Act
            try
            {
                _ = await RoomJoiner.JoinRoom(null, "");
            }
            catch
            {
                // ignored
            }
            finally
            {
                RoomJoiner.JoiningStateChanged -= UpdateLastJoiningState;
            }

            Assert.That(lastRoomJoiningState, Is.InstanceOf<RoomJoiningState.NotJoined>());

            void UpdateLastJoiningState(RoomJoiningState state)
            {
                lastRoomJoiningState = state;
            }
        });

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
