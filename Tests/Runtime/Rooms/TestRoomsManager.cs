using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Castle.Core.Internal;
using Cysharp.Threading.Tasks;
using Elympics.Lobby;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using Elympics.Rooms.Models;
using Elympics.Tests.Common.RoomMocks;
using NUnit.Framework;
using UnityEngine.TestTools;
using static Elympics.Tests.Common.AsyncAsserts;

#nullable enable

namespace Elympics.Tests.Rooms
{
    [Category("Rooms")]
    public class TestRoomsManager
    {
        private readonly RoomsManager _roomsManager;
        private readonly MatchLauncherMock _matchLauncherMock;
        private readonly RoomClientMock _roomsClientMock;
        private readonly RoomJoiningQueueMock _joiningQueueMock;
        private readonly RoomEventObserver _eventRegister;
        private RoomStateChanged _roomStateChanged;
        private readonly Guid _roomIdForTesting = new("10100000000000000000000000000001");
        private CancellationTokenSource _cts;

        public TestRoomsManager()
        {
            _matchLauncherMock = new MatchLauncherMock();
            _roomsClientMock = new RoomClientMock();
            _joiningQueueMock = new RoomJoiningQueueMock();
            _roomsManager = new RoomsManager(_matchLauncherMock, _roomsClientMock, _joiningQueueMock);
            _eventRegister = new RoomEventObserver(_roomsManager);
            _roomStateChanged = RoomsTestUtility.PrepareInitialRoomState(_roomIdForTesting);
        }

        [SetUp]
        public void SetUp()
        {
            _roomStateChanged = RoomsTestUtility.PrepareInitialRoomState(_roomIdForTesting);
            _cts = new();
            _roomsClientMock.Reset();
            _joiningQueueMock.Reset();
            ((IRoomsManager)_roomsManager).Reset();
            var field = _roomsManager.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Find(x => x.Name == "_initialized");
            field.SetValue(_roomsManager, true);
        }

        [Test]
        public void TestRoomRegistration()
        {
            Assert.AreEqual(0, _roomsManager.ListJoinedRooms().Count);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            var joinedRooms = _roomsManager.ListJoinedRooms();

            Assert.AreEqual(1, joinedRooms.Count);
            Assert.AreEqual(_roomIdForTesting, joinedRooms[0].RoomId);
        }

        [Test]
        public void TestJoinedRoomInvoked()
        {
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomInvoked, RoomEventObserver.JoinedRoomUpdatedInvoked);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
        }

        [UnityTest]
        public IEnumerator JoiningNotJoinedButListedRoomShouldSucceed() => UniTask.ToCoroutine(async () =>
        {
            _roomsClientMock.InvokeRoomListChanged(new RoomListChanged(new List<ListedRoomChange>
            {
                new(_roomIdForTesting, RoomsTestUtility.PrepareNotJoinedRoomState(_roomIdForTesting)),
            }));

            _joiningQueueMock.AddRoomIdInvoked += OnAddRoomIdToJoiningQueueInvoked;
            _roomsClientMock.RoomIdReturnTask = UniTask.FromResult(_roomIdForTesting);

            // Act
            var joinedRoom = await _roomsManager.JoinRoom(_roomIdForTesting, null);

            Assert.That(joinedRoom.RoomId, Is.EqualTo(_roomIdForTesting));
            Assert.That(joinedRoom.IsJoined, Is.True);

            void OnAddRoomIdToJoiningQueueInvoked((Guid, bool) args)
            {
                var (_, isAfterResponseFromJoinRoom) = args;
                if (isAfterResponseFromJoinRoom)
                    _roomsClientMock.InvokeRoomStateChanged(RoomsTestUtility.PrepareInitialRoomState(_roomIdForTesting));
            }
        });

        [UnityTest]
        public IEnumerator JoiningAlreadyJoinedRoomShouldResultInException() => UniTask.ToCoroutine(async () =>
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            // Act
            var task = _roomsManager.JoinRoom(_roomIdForTesting, null);

            var exception = await AssertThrowsAsync<RoomAlreadyJoinedException>(task);
            Assert.That(exception.RoomId, Is.EqualTo(_roomIdForTesting));
            Assert.That(exception.JoinCode, Is.Null);
            Assert.That(exception.InProgress, Is.False);
        });

        [UnityTest]
        public IEnumerator JoiningRoomThatIsAlreadyBeingJoinedWithRoomIdShouldResultInException() => UniTask.ToCoroutine(async () =>
        {
            _roomsClientMock.RoomIdReturnTask = UniTask.Delay(TimeSpan.FromSeconds(10), DelayType.DeltaTime, cancellationToken: _cts.Token).ContinueWith(() => _roomIdForTesting);
            _roomsManager.JoinRoom(_roomIdForTesting, null).Forget(_ => { });
            await UniTask.WaitUntil(() => _roomsClientMock.JoinRoomWithRoomIdInvokedArgs.HasValue);

            // Act
            var task = _roomsManager.JoinRoom(_roomIdForTesting, null);

            var exception = await AssertThrowsAsync<RoomAlreadyJoinedException>(task);
            Assert.That(exception.RoomId, Is.EqualTo(_roomIdForTesting));
            Assert.That(exception.JoinCode, Is.Null);
            Assert.That(exception.InProgress, Is.True);
        });

        [UnityTest]
        public IEnumerator JoiningRoomThatIsAlreadyBeingJoinedWithJoinCodeShouldResultInException() => UniTask.ToCoroutine(async () =>
        {
            const string testJoinCode = "testJoinCode";

            _roomsClientMock.RoomIdReturnTask = UniTask.Delay(TimeSpan.FromSeconds(10), DelayType.DeltaTime, cancellationToken: _cts.Token).ContinueWith(() => _roomIdForTesting);
            _roomsManager.JoinRoom(null, testJoinCode).Forget(_ => { });
            await UniTask.WaitUntil(() => _roomsClientMock.JoinRoomWithJoinCodeInvokedArgs.HasValue);

            // Act
            var task = _roomsManager.JoinRoom(null, testJoinCode);

            var exception = await AssertThrowsAsync<RoomAlreadyJoinedException>(task);
            Assert.That(exception.RoomId.HasValue, Is.False);
            Assert.That(exception.JoinCode, Is.EqualTo(testJoinCode));
            Assert.That(exception.InProgress, Is.True);
        });

        [Test]
        public void JoinRoomThatIsAlreadyListedInPublicRoomListAndRoomStateIsUpdated()
        {
            var currentUser = new UserInfo(Guid.NewGuid(), 0, false, string.Empty);
            var currentMmData = new PublicMatchmakingData(DateTime.UnixEpoch, MatchmakingState.Unlocked, "test queue name", 2, 2, new Dictionary<string, string>());
            var roomListChanged = new RoomListChanged(new List<ListedRoomChange>
            {
                new(_roomIdForTesting,
                    new PublicRoomState(_roomIdForTesting,
                        _roomStateChanged.LastUpdate,
                        _roomStateChanged.RoomName,
                        true,
                        currentMmData,
                        new List<UserInfo>
                        {
                            currentUser,
                        },
                        false,
                        new Dictionary<string, string>())),
            });

            // Act
            _roomsClientMock.InvokeRoomListChanged(roomListChanged);
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.JoinedRoomInvoked);
            var roomStateWithUsers = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                Users = new List<UserInfo>()
                {
                    currentUser,
                    _roomStateChanged.Users[0]
                },
                MatchmakingData = new MatchmakingData(currentMmData.LastStateUpdate, currentMmData.State, currentMmData.QueueName, currentMmData.TeamCount, currentMmData.TeamSize, currentMmData.CustomData, null)
            };
            _roomsClientMock.InvokeRoomStateChanged(roomStateWithUsers);

            //Assert
            _eventRegister.AssertIfInvoked();
            Assert.AreEqual(2, _roomsManager.ListJoinedRooms()[0].State.Users.Count());
        }

        [UnityTest]
        public IEnumerator HappyPathStartingQuickMatchShouldSucceed() => UniTask.ToCoroutine(async () =>
        {
            const string regionName = "test-region";
            var connectionDetails = new SessionConnectionDetails("url", new AuthData(Guid.Empty, "", ""), Guid.Empty, "", regionName);
            _roomsClientMock.SetSessionConnectionDetails(connectionDetails);
            _roomsClientMock.RoomIdReturnTask = UniTask.FromResult(_roomIdForTesting);

            var teamChangedState = _roomStateChanged with
            {
                Users = new List<UserInfo>
                {
                    new(Guid.Empty, 0, false, string.Empty),
                },
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1)
            };

            var readyState = teamChangedState with
            {
                Users = new List<UserInfo>
                {
                    new(Guid.Empty, 0, true, string.Empty),
                },
                LastUpdate = teamChangedState.LastUpdate + TimeSpan.FromSeconds(1),
            };

            var matchmakingState = readyState with
            {
                MatchmakingData = readyState.MatchmakingData! with
                {
                    State = MatchmakingState.RequestingMatchmaking,
                    LastStateUpdate = readyState.LastUpdate + TimeSpan.FromSeconds(1),
                },
                LastUpdate = readyState.LastUpdate + TimeSpan.FromSeconds(1),
            };

            var matchDataState = matchmakingState with
            {
                MatchmakingData = matchmakingState.MatchmakingData with
                {
                    MatchData = new MatchData(Guid.Empty, MatchState.Running, new MatchDetails(matchmakingState.Users.Select(x => x.UserId).ToList(), string.Empty, string.Empty, string.Empty, new byte[] { }, new float[] { }), string.Empty)
                },
                LastUpdate = matchmakingState.LastUpdate + TimeSpan.FromSeconds(1),
            };

            _joiningQueueMock.AddRoomIdInvoked += _ => _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _roomsClientMock.SetReadyInvoked += _ => _roomsClientMock.InvokeRoomStateChanged(readyState);
            _roomsClientMock.SetTeamChangedInvoked += _ => _roomsClientMock.InvokeRoomStateChanged(teamChangedState);
            _roomsClientMock.StartMatchmakingInvoked += _ => MatchmakingFlow().Forget();

            // Act
            _ = await _roomsManager.StartQuickMatch("", Array.Empty<byte>(), Array.Empty<float>());

            async UniTask MatchmakingFlow()
            {
                _roomsClientMock.InvokeRoomStateChanged(matchmakingState);
                await UniTask.Delay(TimeSpan.FromSeconds(1));
                _roomsClientMock.InvokeRoomStateChanged(matchDataState);
            }

        });


        [Test]
        public void RoomListUpdatedEventShouldBeInvokedAfterReceivingUpdatedRoomList()
        {
            var roomListChanged = new RoomListChanged(new List<ListedRoomChange>
            {
                new(_roomIdForTesting,
                    new PublicRoomState(_roomIdForTesting,
                        DateTime.UnixEpoch,
                        "test room name",
                        true,
                        new PublicMatchmakingData(DateTime.UnixEpoch, MatchmakingState.Unlocked, "test queue name", 2, 2, new Dictionary<string, string>()),
                        new List<UserInfo>
                        {
                            new(Guid.NewGuid(), 0, false, string.Empty)
                        },
                        false,
                        new Dictionary<string, string>())),
            });
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.RoomListUpdatedInvoked);

            // Act
            _roomsClientMock.InvokeRoomListChanged(roomListChanged);

            _eventRegister.AssertIfInvoked();
        }

        [Test]
        public void AvailableRoomListShouldBeExpandedCorrectlyWhenRoomListUpdateIsReceived()
        {
            var roomState = new PublicRoomState(_roomIdForTesting,
                DateTime.UnixEpoch,
                "test room name",
                true,
                new PublicMatchmakingData(DateTime.UnixEpoch, MatchmakingState.Unlocked, "test queue name", 2, 2, new Dictionary<string, string>()),
                new List<UserInfo>
                {
                    new(Guid.Empty, 0, false, string.Empty)
                },
                false,
                new Dictionary<string, string>());
            var roomListChanged = new RoomListChanged(new List<ListedRoomChange>
            {
                new(_roomIdForTesting, roomState),
            });

            // Act
            _roomsClientMock.InvokeRoomListChanged(roomListChanged);

            Assert.That(_roomsManager.ListAvailableRooms().Count, Is.EqualTo(1));
            Assert.That(_roomsManager.ListAvailableRooms()[0].RoomId, Is.EqualTo(_roomIdForTesting));
            Assert.That(_roomsManager.ListAvailableRooms()[0].State.RoomName, Is.EqualTo(roomState.RoomName));
        }

        [Test]
        public void AvailableRoomListShouldBeReducedCorrectlyWhenRoomListUpdateIsReceived()
        {
            var roomState = new PublicRoomState(_roomIdForTesting,
                DateTime.UnixEpoch,
                "test room name",
                true,
                new PublicMatchmakingData(DateTime.UnixEpoch, MatchmakingState.Unlocked, "test queue name", 2, 2, new Dictionary<string, string>()),
                new List<UserInfo>
                {
                    new(Guid.Empty, 0, false, string.Empty)
                },
                false,
                new Dictionary<string, string>());
            _roomsClientMock.InvokeRoomListChanged(new RoomListChanged(new List<ListedRoomChange>
            {
                new(_roomIdForTesting, roomState),
            }));

            var roomListChanged = new RoomListChanged(new List<ListedRoomChange>
            {
                new(_roomIdForTesting, null),
            });

            // Act
            _roomsClientMock.InvokeRoomListChanged(roomListChanged);

            Assert.That(_roomsManager.ListAvailableRooms().Count, Is.EqualTo(0));
        }

        [Test]
        public void AvailableRoomListShouldBeModifiedCorrectlyWhenRoomListUpdateIsReceived()
        {
            var roomState = new PublicRoomState(_roomIdForTesting,
                DateTime.UnixEpoch,
                "test room name",
                true,
                new PublicMatchmakingData(DateTime.UnixEpoch, MatchmakingState.Unlocked, "test queue name", 2, 2, new Dictionary<string, string>()),
                new List<UserInfo>
                {
                    new(Guid.Empty, 0, false, string.Empty)
                },
                false,
                new Dictionary<string, string>());
            _roomsClientMock.InvokeRoomListChanged(new RoomListChanged(new List<ListedRoomChange>
            {
                new(_roomIdForTesting, roomState),
            }));

            var modifiedState = new PublicRoomState(_roomIdForTesting,
                DateTime.UnixEpoch + TimeSpan.FromSeconds(1),
                "test room name modified",
                true,
                new PublicMatchmakingData(DateTime.UnixEpoch + TimeSpan.FromSeconds(1), MatchmakingState.Unlocked, "test queue name", 2, 2, new Dictionary<string, string>()),
                new List<UserInfo>
                {
                    new(Guid.Empty, 0, false, string.Empty)
                },
                false,
                new Dictionary<string, string>());
            var roomListChanged = new RoomListChanged(new List<ListedRoomChange>
            {
                new(_roomIdForTesting, modifiedState),
            });

            // Act
            _roomsClientMock.InvokeRoomListChanged(roomListChanged);

            Assert.That(_roomsManager.ListAvailableRooms().Count, Is.EqualTo(1));
            Assert.That(_roomsManager.ListAvailableRooms()[0].RoomId, Is.EqualTo(_roomIdForTesting));
            Assert.That(_roomsManager.ListAvailableRooms()[0].State.RoomName, Is.EqualTo(modifiedState.RoomName));
        }

        [Test]
        public void TestJoinedRoomUpdatedInvoked()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
        }

        [Test]
        public void TestRoomStateChangedDoNotRoomsCount()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            Assert.AreEqual(1, _roomsManager.ListJoinedRooms().Count);
            Assert.AreEqual(_roomIdForTesting, _roomsManager.ListJoinedRooms()[0].RoomId);
        }

        [Test]
        public void TestRoomStateChangedNotInvokedDueToLateRoomStateChange()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate - TimeSpan.FromSeconds(1),
            };
            _eventRegister.Reset();
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
        }

        [Test]
        public void TestUserJoinedInvoked()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.UserJoinedInvoked, RoomEventObserver.UserCountChangedInvoked);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                Users = _roomStateChanged.Users.Append(new UserInfo(Guid.NewGuid(), 0, false, string.Empty)).ToList(),
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
        }

        [Test]
        public void TestUserLeftInvoked()
        {
            _roomStateChanged = _roomStateChanged with
            {
                Users = _roomStateChanged.Users.Append(new UserInfo(Guid.NewGuid(), 0, false, string.Empty)).ToList(),
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.UserLeftInvoked, RoomEventObserver.UserCountChangedInvoked);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                Users = _roomStateChanged.Users.Take(_roomStateChanged.Users.Count - 1).ToList(),
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
        }

        [Test]
        public void TestHostChangedInvoked()
        {
            _roomStateChanged = _roomStateChanged with
            {
                Users = _roomStateChanged.Users.Append(new UserInfo(Guid.NewGuid(), 0, false, string.Empty)).ToList(),
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.HostChangedInvoked);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                Users = _roomStateChanged.Users.Reverse().ToList(),
            };
            var newHost = _roomStateChanged.Users[0].UserId;
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.AreEqual(newHost, _roomsManager.ListJoinedRooms()[0].State.Host.UserId);
        }

        [Test]
        public void TestUserReadinessChangedToReadyInvoked()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.UserReadinessChangedInvoked);
            var readyUser = _roomStateChanged.Users[0] with
            {
                IsReady = true,
            };
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                Users = _roomStateChanged.Users.Skip(1).Prepend(readyUser).ToList(),
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.IsTrue(_roomsManager.ListJoinedRooms()[0].State.Users[0].IsReady);
        }

        [Test]
        public void TestUserReadinessChangedToUnreadyInvoked()
        {
            var readyUser = _roomStateChanged.Users[0] with
            {
                IsReady = true,
            };
            _roomStateChanged = _roomStateChanged with
            {
                Users = _roomStateChanged.Users.Skip(1).Prepend(readyUser).ToList(),
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.UserReadinessChangedInvoked);
            var unreadyUser = _roomStateChanged.Users[0] with
            {
                IsReady = false,
            };
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                Users = _roomStateChanged.Users.Skip(1).Prepend(unreadyUser).ToList(),
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.IsFalse(_roomsManager.ListJoinedRooms()[0].State.Users[0].IsReady);
        }

        [Test]
        public void TestTeamChangedInvoked()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.UserChangedTeamInvoked);
            var userWithChangedTeam = _roomStateChanged.Users[0] with
            {
                TeamIndex = 1,
            };
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                Users = _roomStateChanged.Users.Skip(1).Prepend(userWithChangedTeam).ToList(),
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.AreEqual(1, _roomsManager.ListJoinedRooms()[0].State.Users[0].TeamIndex);
        }

        [Test]
        public void TestCustomDataChangedInvoked()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            const string newDataKey = "testKey";
            const string testValue = "testValue";
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.CustomRoomDataChangedInvoked);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                CustomData = new Dictionary<string, string>()
                {
                    {
                        newDataKey, testValue
                    },
                },
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.IsTrue(_roomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newDataKey));
            Assert.AreSame(testValue, _roomsManager.ListJoinedRooms()[0].State.CustomData[newDataKey]);
        }

        [Test]
        public void TestCustomDataChangedInvokedTwice()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.CustomRoomDataChangedInvoked, RoomEventObserver.CustomRoomDataChangedInvoked);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                CustomData = new Dictionary<string, string>
                {
                    {
                        newKey1, newValue1
                    },
                    {
                        newKey2, newValue2
                    },
                },
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.IsTrue(_roomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey1));
            Assert.IsTrue(_roomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey2));
            Assert.AreSame(newValue1, _roomsManager.ListJoinedRooms()[0].State.CustomData[newKey1]);
            Assert.AreSame(newValue2, _roomsManager.ListJoinedRooms()[0].State.CustomData[newKey2]);
        }

        [Test]
        public void TestCustomDataChangedInvokedTwiceWhenRemovedAll()
        {
            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                CustomData = new Dictionary<string, string>
                {
                    {
                        newKey1, newValue1
                    },
                    {
                        newKey2, newValue2
                    },
                },
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.CustomRoomDataChangedInvoked, RoomEventObserver.CustomRoomDataChangedInvoked);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                CustomData = new Dictionary<string, string>(),
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.IsFalse(_roomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey1));
            Assert.IsFalse(_roomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey2));
        }


        [Test]
        public void TestCustomDataChangedOnceWhenAddOneKey()
        {
            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                CustomData = new Dictionary<string, string>
                {
                    {
                        newKey1, newValue1
                    },
                },
            };

            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                CustomData = new Dictionary<string, string>
                {
                    {
                        newKey1, newValue1
                    },
                    {
                        newKey2, newValue2
                    },
                },
            };

            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.CustomRoomDataChangedInvoked);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.True(_roomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey1));
            Assert.True(_roomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey2));
            Assert.AreSame(newValue1, _roomsManager.ListJoinedRooms()[0].State.CustomData[newKey1]);
            Assert.AreSame(newValue2, _roomsManager.ListJoinedRooms()[0].State.CustomData[newKey2]);
        }

        [Test]
        public void TestCustomDataChangedTwiceWhenUpdateOneKey()
        {
            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";

            const string newKey3 = "testKey3";
            const string newValue3 = "testValue3";

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                CustomData = new Dictionary<string, string>
                {
                    {
                        newKey1, newValue1
                    },
                    {
                        newKey2, newValue2
                    },
                },
            };

            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                CustomData = new Dictionary<string, string>
                {
                    {
                        newKey1, newValue1
                    },
                    {
                        newKey3, newValue3
                    },
                },
            };

            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.CustomRoomDataChangedInvoked, RoomEventObserver.CustomRoomDataChangedInvoked);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.True(_roomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey1));
            Assert.False(_roomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey2));
            Assert.True(_roomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey3));
            Assert.AreSame(newValue1, _roomsManager.ListJoinedRooms()[0].State.CustomData[newKey1]);
            Assert.AreSame(newValue3, _roomsManager.ListJoinedRooms()[0].State.CustomData[newKey3]);
        }


        [Test]
        public void TestRoomLeftInvoked()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.LeftRoomInvoked);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
            };
            var leaveRoomArgs = new LeftRoomArgs(_roomStateChanged.RoomId, LeavingReason.RoomClosed);
            _roomsClientMock.InvokeLeftRoom(leaveRoomArgs);
            _eventRegister.AssertIfInvoked();
        }

        [Test]
        public void TestRoomAfterLeftRoom()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            var leaveRoomArgs = new LeftRoomArgs(_roomStateChanged.RoomId, LeavingReason.RoomClosed);
            _roomsClientMock.InvokeLeftRoom(leaveRoomArgs);
            Assert.AreEqual(0, _roomsManager.ListJoinedRooms().Count);
        }

        private static List<(MatchmakingData?, MatchmakingData)> matchmakingStartedTestCases = new()
        {
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Unlocked), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.RequestingMatchmaking)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Unlocked), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matchmaking)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Unlocked), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.CancellingMatchmaking)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Playing), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.RequestingMatchmaking)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Playing), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matchmaking)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Playing), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.CancellingMatchmaking)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matched), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.RequestingMatchmaking)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matched), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matchmaking)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matched), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.CancellingMatchmaking)),
            (null, RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.RequestingMatchmaking)),
            (null, RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matchmaking)),
            (null, RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.CancellingMatchmaking)),
        };


        [Test]
        public void TestMatchmakingStartedInvokedAndMatchmakingMatchmakingStateChangedInvoked([ValueSource(nameof(matchmakingStartedTestCases))] (MatchmakingData cachedState, MatchmakingData newState) statesTuple)
        {
            _roomStateChanged = _roomStateChanged with
            {
                MatchmakingData = statesTuple.cachedState,
            };

            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = statesTuple.newState with
                {
                    LastStateUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                    MatchData = null,
                },
            };
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.MatchmakingDataChangedInvoked, RoomEventObserver.MatchmakingStartedInvoked);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
        }

        private static List<(MatchmakingData, MatchmakingData?)> matchmakingEndedTestCases = new()
        {
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.RequestingMatchmaking), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Unlocked)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.RequestingMatchmaking), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Playing)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.RequestingMatchmaking), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matched)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.RequestingMatchmaking), null),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matchmaking), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Unlocked)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matchmaking), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Playing)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matchmaking), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matched)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matchmaking), null),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.RequestingMatchmaking), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Unlocked)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.RequestingMatchmaking), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Playing)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.RequestingMatchmaking), RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.Matched)),
            (RoomsTestUtility.GetMatchmakingDataForState(MatchmakingState.RequestingMatchmaking), null),
        };

        [Test]
        public void TestMatchmakingEndedAndMatchmakingMatchmakingStateChangedInvoked([ValueSource(nameof(matchmakingEndedTestCases))] (MatchmakingData? cachedState, MatchmakingData? newState) statesTuple)
        {
            var (cachedState, newState) = statesTuple;
            if (newState != null)
                newState = newState with
                {
                    LastStateUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                };

            _roomStateChanged = _roomStateChanged with
            {
                MatchmakingData = cachedState,
            };

            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = newState,
            };

            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.MatchmakingDataChangedInvoked, RoomEventObserver.MatchmakingEndedInvoked);

            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _eventRegister.AssertIfInvoked();
        }


        [Test]
        public void TestMatchDataReceivedAndMatchmakingMatchmakingStateChangedInvoked()
        {
            _roomStateChanged = _roomStateChanged with
            {
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    MatchData = null,
                },
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    MatchData = RoomsTestUtility.GetDummyMatchData(new List<Guid>()),
                    LastStateUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                },
            };
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.MatchmakingDataChangedInvoked, RoomEventObserver.MatchDataReceivedInvoked);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
        }

        [Test]
        public void TestCustomMatchmakingDataChanged()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            const string newKey = "testKey";
            const string newValue = "testValue";
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    CustomData = new Dictionary<string, string>
                    {
                        {
                            newKey, newValue
                        },
                    },
                },
            };
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.MatchmakingDataChangedInvoked, RoomEventObserver.CustomMatchmakingDataChanged);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.IsTrue(_roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey));
            Assert.AreSame(newValue, _roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey]);
        }

        [Test]
        public void TestCustomMatchmakingDataChangedTwice()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    CustomData = new Dictionary<string, string>()
                    {
                        {
                            newKey1, newValue1
                        },
                        {
                            newKey2, newValue2
                        },
                    },
                },
            };
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.MatchmakingDataChangedInvoked, RoomEventObserver.CustomMatchmakingDataChanged, RoomEventObserver.CustomMatchmakingDataChanged);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.IsTrue(_roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey1));
            Assert.IsTrue(_roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey2));
            Assert.AreSame(newValue1, _roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey1]);
            Assert.AreSame(newValue2, _roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey2]);
        }

        [Test]
        public void TestCustomMatchmakingDataChangedTwiceWhenRemovedAll()
        {
            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    CustomData = new Dictionary<string, string>
                    {
                        {
                            newKey1, newValue1
                        },
                        {
                            newKey2, newValue2
                        },
                    },
                },
            };

            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    CustomData = new Dictionary<string, string>(),
                },
            };

            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.MatchmakingDataChangedInvoked, RoomEventObserver.CustomMatchmakingDataChanged, RoomEventObserver.CustomMatchmakingDataChanged);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.False(_roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey1));
            Assert.False(_roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey2));
        }

        [Test]
        public void TestCustomMatchmakingDataChangedOnceWhenAddOneKey()
        {
            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    CustomData = new Dictionary<string, string>
                    {
                        {
                            newKey1, newValue1
                        },
                    },
                },
            };

            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    CustomData = new Dictionary<string, string>()
                    {
                        {
                            newKey1, newValue1
                        },
                        {
                            newKey2, newValue2
                        },
                    },
                },
            };

            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.MatchmakingDataChangedInvoked, RoomEventObserver.CustomMatchmakingDataChanged);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.True(_roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey1));
            Assert.True(_roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey2));
            Assert.AreSame(newValue1, _roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey1]);
            Assert.AreSame(newValue2, _roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey2]);
        }

        [Test]
        public void TestCustomMatchmakingDataChangedTwiceWhenUpdateOneKey()
        {
            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";

            const string newKey3 = "testKey3";
            const string newValue3 = "testValue3";

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    CustomData = new Dictionary<string, string>
                    {
                        {
                            newKey1, newValue1
                        },
                        {
                            newKey2, newValue2
                        },
                    },
                },
            };

            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    CustomData = new Dictionary<string, string>()
                    {
                        {
                            newKey1, newValue1
                        },
                        {
                            newKey3, newValue3
                        },
                    },
                },
            };

            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.MatchmakingDataChangedInvoked, RoomEventObserver.CustomMatchmakingDataChanged, RoomEventObserver.CustomMatchmakingDataChanged);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.True(_roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey1));
            Assert.False(_roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey2));
            Assert.True(_roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey3));
            Assert.AreSame(newValue1, _roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey1]);
            Assert.AreSame(newValue3, _roomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey3]);
        }

        [Test]
        public void TestRoomNameChangedInvoked()
        {
            const string newRoomName = "NewRoomName";
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                RoomName = newRoomName,
            };
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.RoomNameChangedInvoked);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.AreSame(newRoomName, _roomsManager.ListJoinedRooms()[0].State.RoomName);
        }

        [Test]
        public void TestIsPrivateChangedInvoked()
        {
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                IsPrivate = true,
            };
            _eventRegister.ResetInvocationStatusAndRegisterAssertion(RoomEventObserver.JoinedRoomUpdatedInvoked, RoomEventObserver.RoomPublicnessChangedInvoked);
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);
            _eventRegister.AssertIfInvoked();
            Assert.AreEqual(true, _roomsManager.ListJoinedRooms()[0].State.IsPrivate);
        }

        [Test]
        public void PlayMatchShouldBeCalledAfterReceivingMatchDataIfGameplaySceneIsSetToBeLoadedAfterMatchmaking()
        {
            _roomStateChanged = _roomStateChanged with
            {
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    MatchData = null,
                },
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            var matchData = RoomsTestUtility.GetDummyMatchData(_roomStateChanged.Users.Select(x => x.UserId).ToList());
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    State = MatchmakingState.Playing,
                    MatchData = matchData,
                    LastStateUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                },
            };

            const string regionName = "test-region";
            var connectionDetails = new SessionConnectionDetails("url", new AuthData(Guid.Empty, "", ""), Guid.Empty, "", regionName);
            _roomsClientMock.SetSessionConnectionDetails(connectionDetails);
            _matchLauncherMock.ShouldLoadGameplaySceneAfterMatchmaking = true;

            // Act
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            Assert.That(_matchLauncherMock.PlayMatchCalledArgs, Is.Not.Null);
            Assert.That(_matchLauncherMock.PlayMatchCalledArgs, Is.EqualTo(new MatchmakingFinishedData(matchData.MatchId, matchData.MatchDetails!, _roomStateChanged.MatchmakingData.QueueName, regionName)));
        }

        [Test]
        public void PlayMatchShouldNotBeCalledAfterReceivingMatchDataIfGameplaySceneIsNotSetToBeLoadedAfterMatchmaking()
        {
            _roomStateChanged = _roomStateChanged with
            {
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    MatchData = null,
                },
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            var matchData = RoomsTestUtility.GetDummyMatchData(_roomStateChanged.Users.Select(x => x.UserId).ToList());
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    State = MatchmakingState.Playing,
                    MatchData = matchData,
                    LastStateUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                },
            };

            _matchLauncherMock.ShouldLoadGameplaySceneAfterMatchmaking = false;

            // Act
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            Assert.That(_matchLauncherMock.PlayMatchCalledArgs, Is.Null);
        }

        [Test]
        public void PlayMatchShouldNotBeCalledAfterReceivingMatchDataIfGameplaySceneIsSetToBeLoadedAfterMatchmakingButMatchIsAlreadyRunning()
        {
            _roomStateChanged = _roomStateChanged with
            {
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    MatchData = null,
                },
            };
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            var matchData = RoomsTestUtility.GetDummyMatchData(_roomStateChanged.Users.Select(x => x.UserId).ToList());
            _roomStateChanged = _roomStateChanged with
            {
                LastUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = _roomStateChanged.MatchmakingData! with
                {
                    State = MatchmakingState.Playing,
                    MatchData = matchData,
                    LastStateUpdate = _roomStateChanged.LastUpdate + TimeSpan.FromSeconds(1),
                },
            };

            _matchLauncherMock.ShouldLoadGameplaySceneAfterMatchmaking = true;
            _matchLauncherMock.IsCurrentlyInMatch = true;

            // Act
            _roomsClientMock.InvokeRoomStateChanged(_roomStateChanged);

            Assert.That(_matchLauncherMock.PlayMatchCalledArgs, Is.Null);
        }

        [TearDown]
        public void Reset()
        {
            _cts?.Cancel();
            _eventRegister.Reset();
            ((IRoomsManager)_roomsManager).Reset();
            _matchLauncherMock.Reset();
        }
    }
}
