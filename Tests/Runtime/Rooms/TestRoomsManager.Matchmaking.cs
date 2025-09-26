using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Rooms.InternalModels;
using Elympics.Communication.Rooms.InternalModels.FromRooms;
using Elympics.Lobby;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using Elympics.Rooms.Models;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.TestTools;

#nullable enable

namespace Elympics.Tests.Rooms
{
    [Category("Rooms")]
    internal class TestRoomsManager_Matchmaking : TestRoomsManager
    {

        private static List<(MatchmakingData?, MatchmakingData)> matchmakingStartedTestCases = new()
        {
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.Unlocked), Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking)),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.Unlocked), Defaults.CreateMatchmakingData(MatchmakingStateDto.Matchmaking)),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.Unlocked), Defaults.CreateMatchmakingData(MatchmakingStateDto.CancellingMatchmaking)),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.Playing), Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking)),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.Playing), Defaults.CreateMatchmakingData(MatchmakingStateDto.Matchmaking)),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.Playing), Defaults.CreateMatchmakingData(MatchmakingStateDto.CancellingMatchmaking)),
            (null, Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking)),
            (null, Defaults.CreateMatchmakingData(MatchmakingStateDto.Matchmaking)),
            (null, Defaults.CreateMatchmakingData(MatchmakingStateDto.CancellingMatchmaking)),
        };


        [Test]
        public void TestMatchmakingStartedInvokedAndMatchmakingMatchmakingStateChangedInvoked(
            [ValueSource(nameof(matchmakingStartedTestCases))]
            (MatchmakingData cachedState, MatchmakingData newState) statesTuple)
        {
            var matchmakingRoomState = InitialRoomState with
            {
                MatchmakingData = statesTuple.cachedState,
            };

            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = statesTuple.newState with
                {
                    LastStateUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                    MatchData = null,
                },
            };
            EventRegister.ListenForEvents(
                nameof(IRoomsManager.JoinedRoomUpdated),
                nameof(IRoomsManager.MatchmakingDataChanged),
                nameof(IRoomsManager.MatchmakingStarted));
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
        }

        private static List<(MatchmakingData, MatchmakingData?)> matchmakingEndedTestCases = new()
        {
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking), Defaults.CreateMatchmakingData(MatchmakingStateDto.Unlocked)),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking), Defaults.CreateMatchmakingData(MatchmakingStateDto.Playing)),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking), null),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.Matchmaking), Defaults.CreateMatchmakingData(MatchmakingStateDto.Unlocked)),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.Matchmaking), Defaults.CreateMatchmakingData(MatchmakingStateDto.Playing)),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.Matchmaking), null),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking), Defaults.CreateMatchmakingData(MatchmakingStateDto.Unlocked)),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking), Defaults.CreateMatchmakingData(MatchmakingStateDto.Playing)),
            (Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking), null),
        };

        [Test]
        public void TestMatchmakingEndedAndMatchmakingMatchmakingStateChangedInvoked(
            [ValueSource(nameof(matchmakingEndedTestCases))]
            (MatchmakingData? cachedState, MatchmakingData? newState) statesTuple)
        {
            var (cachedState, newState) = statesTuple;
            if (newState != null)
                newState = newState with
                {
                    LastStateUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                };

            var matchmakingRoomState = InitialRoomState with
            {
                MatchmakingData = cachedState,
            };

            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = newState,
            };

            EventRegister.ListenForEvents(
                nameof(IRoomsManager.JoinedRoomUpdated),
                nameof(IRoomsManager.MatchmakingDataChanged),
                nameof(IRoomsManager.MatchmakingEnded));

            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            EventRegister.AssertIfInvoked(false);
        }

        [Test]
        public void TestMatchDataReceivedAndMatchmakingMatchmakingStateChangedInvoked()
        {
            var matchmakingRoomState = InitialRoomState with
            {
                MatchmakingData = InitialRoomState.MatchmakingData! with
                {
                    MatchData = null,
                },
            };
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = matchmakingRoomState.MatchmakingData! with
                {
                    MatchData = Defaults.CreateMatchData(new List<Guid>()),
                    LastStateUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                },
            };
            EventRegister.ListenForEvents(
                nameof(IRoomsManager.JoinedRoomUpdated),
                nameof(IRoomsManager.MatchmakingDataChanged),
                nameof(IRoomsManager.MatchDataReceived));
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
        }

        [Test]
        public void TestCustomMatchmakingDataChanged()
        {
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(InitialRoomState);
            const string newKey = "testKey";
            const string newValue = "testValue";
            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = InitialRoomState.MatchmakingData! with
                {
                    CustomData = new Dictionary<string, string>
                    {
                        {
                            newKey, newValue
                        },
                    },
                },
            };
            EventRegister.ListenForEvents(
                nameof(IRoomsManager.JoinedRoomUpdated),
                nameof(IRoomsManager.MatchmakingDataChanged),
                nameof(IRoomsManager.CustomMatchmakingDataChanged));
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.IsTrue(RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey));
            Assert.AreSame(newValue, RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey]);
        }

        [Test]
        public void TestCustomMatchmakingDataChangedTwice()
        {
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(InitialRoomState);
            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";

            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = InitialRoomState.MatchmakingData! with
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
            EventRegister.ListenForEvents(
                nameof(IRoomsManager.JoinedRoomUpdated),
                nameof(IRoomsManager.MatchmakingDataChanged),
                nameof(IRoomsManager.CustomMatchmakingDataChanged),
                nameof(IRoomsManager.CustomMatchmakingDataChanged));
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.IsTrue(RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey1));
            Assert.IsTrue(RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey2));
            Assert.AreSame(newValue1, RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey1]);
            Assert.AreSame(newValue2, RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey2]);
        }

        [Test]
        public void TestCustomMatchmakingDataChangedTwiceWhenRemovedAll()
        {
            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";

            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = InitialRoomState.MatchmakingData! with
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

            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = matchmakingRoomState.MatchmakingData! with
                {
                    CustomData = new Dictionary<string, string>(),
                },
            };

            EventRegister.ListenForEvents(
                nameof(IRoomsManager.JoinedRoomUpdated),
                nameof(IRoomsManager.MatchmakingDataChanged),
                nameof(IRoomsManager.CustomMatchmakingDataChanged),
                nameof(IRoomsManager.CustomMatchmakingDataChanged));
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.False(RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey1));
            Assert.False(RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey2));
        }

        [Test]
        public void TestCustomMatchmakingDataChangedOnceWhenAddOneKey()
        {
            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";

            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = InitialRoomState.MatchmakingData! with
                {
                    CustomData = new Dictionary<string, string>
                    {
                        {
                            newKey1, newValue1
                        },
                    },
                },
            };

            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = matchmakingRoomState.MatchmakingData! with
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

            EventRegister.ListenForEvents(
                nameof(IRoomsManager.JoinedRoomUpdated),
                nameof(IRoomsManager.MatchmakingDataChanged),
                nameof(IRoomsManager.CustomMatchmakingDataChanged));
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.True(RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey1));
            Assert.True(RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey2));
            Assert.AreSame(newValue1, RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey1]);
            Assert.AreSame(newValue2, RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey2]);
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

            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = InitialRoomState.MatchmakingData! with
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

            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = matchmakingRoomState.MatchmakingData! with
                {
                    CustomData = new Dictionary<string, string>
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

            EventRegister.ListenForEvents(
                nameof(IRoomsManager.JoinedRoomUpdated),
                nameof(IRoomsManager.MatchmakingDataChanged),
                nameof(IRoomsManager.CustomMatchmakingDataChanged),
                nameof(IRoomsManager.CustomMatchmakingDataChanged));
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.True(RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey1));
            Assert.False(RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey2));
            Assert.True(RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData.ContainsKey(newKey3));
            Assert.AreSame(newValue1, RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey1]);
            Assert.AreSame(newValue3, RoomsManager.ListJoinedRooms()[0].State.MatchmakingData!.CustomData[newKey3]);
        }

        [Test]
        public void TestCustomDataChangedInvoked()
        {
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(InitialRoomState);

            const string newDataKey = "testKey";
            const string testValue = "testValue";
            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.CustomRoomDataChanged));
            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                CustomData = new Dictionary<string, string>()
                {
                    {
                        newDataKey, testValue
                    },
                },
            };
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.IsTrue(RoomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newDataKey));
            Assert.AreSame(testValue, RoomsManager.ListJoinedRooms()[0].State.CustomData[newDataKey]);
        }

        [Test]
        public void TestCustomDataChangedInvokedTwice()
        {
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(InitialRoomState);

            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";
            EventRegister.ListenForEvents(
                nameof(IRoomsManager.JoinedRoomUpdated),
                nameof(IRoomsManager.CustomRoomDataChanged),
                nameof(IRoomsManager.CustomRoomDataChanged));
            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
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
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.IsTrue(RoomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey1));
            Assert.IsTrue(RoomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey2));
            Assert.AreSame(newValue1, RoomsManager.ListJoinedRooms()[0].State.CustomData[newKey1]);
            Assert.AreSame(newValue2, RoomsManager.ListJoinedRooms()[0].State.CustomData[newKey2]);
        }

        [Test]
        public void TestCustomDataChangedInvokedTwiceWhenRemovedAll()
        {
            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";

            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
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
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.ListenForEvents(
                nameof(IRoomsManager.JoinedRoomUpdated),
                nameof(IRoomsManager.CustomRoomDataChanged),
                nameof(IRoomsManager.CustomRoomDataChanged));
            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                CustomData = new Dictionary<string, string>(),
            };
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.IsFalse(RoomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey1));
            Assert.IsFalse(RoomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey2));
        }


        [Test]
        public void TestCustomDataChangedOnceWhenAddOneKey()
        {
            const string newKey1 = "testKey";
            const string newValue1 = "testValue";

            const string newKey2 = "testKey2";
            const string newValue2 = "testValue2";

            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                CustomData = new Dictionary<string, string>
                {
                    {
                        newKey1, newValue1
                    },
                },
            };

            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
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

            EventRegister.ListenForEvents(nameof(IRoomsManager.JoinedRoomUpdated), nameof(IRoomsManager.CustomRoomDataChanged));
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.True(RoomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey1));
            Assert.True(RoomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey2));
            Assert.AreSame(newValue1, RoomsManager.ListJoinedRooms()[0].State.CustomData[newKey1]);
            Assert.AreSame(newValue2, RoomsManager.ListJoinedRooms()[0].State.CustomData[newKey2]);
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

            var matchmakingRoomState = InitialRoomState with
            {
                LastUpdate = InitialRoomState.LastUpdate + TimeSpan.FromSeconds(1),
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

            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
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

            EventRegister.ListenForEvents(
                nameof(IRoomsManager.JoinedRoomUpdated),
                nameof(IRoomsManager.CustomRoomDataChanged),
                nameof(IRoomsManager.CustomRoomDataChanged));
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);
            EventRegister.AssertIfInvoked();
            Assert.True(RoomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey1));
            Assert.False(RoomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey2));
            Assert.True(RoomsManager.ListJoinedRooms()[0].State.CustomData.ContainsKey(newKey3));
            Assert.AreSame(newValue1, RoomsManager.ListJoinedRooms()[0].State.CustomData[newKey1]);
            Assert.AreSame(newValue3, RoomsManager.ListJoinedRooms()[0].State.CustomData[newKey3]);
        }

        [Test]
        public void PlayMatchShouldBeCalledAfterReceivingMatchDataIfGameplaySceneIsSetToBeLoadedAfterMatchmaking()
        {
            var matchmakingRoomState = InitialRoomState with
            {
                MatchmakingData = InitialRoomState.MatchmakingData! with
                {
                    MatchData = null,
                },
            };
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            var matchData = Defaults.CreateMatchData(matchmakingRoomState.Users.Select(x => x.UserId).ToList());
            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = matchmakingRoomState.MatchmakingData! with
                {
                    State = MatchmakingStateDto.Playing,
                    MatchData = matchData,
                    LastStateUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                },
            };

            const string regionName = "test-region";
            var connectionDetails = new SessionConnectionDetails("url", new AuthData(Guid.Empty, "", ""), Guid.Empty, "", regionName);
            _ = RoomsClientMock.SessionConnectionDetails.Returns(connectionDetails);
            MatchLauncherMock.ShouldLoadGameplaySceneAfterMatchmaking = true;
            MatchmakingFinishedData? playMatchArgs = null;
            MatchLauncherMock.When(x => x.PlayMatch(Arg.Any<MatchmakingFinishedData>()))
                .Do(x => playMatchArgs = x.ArgAt<MatchmakingFinishedData>(0));

            // Act
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            Assert.That(playMatchArgs, Is.Not.Null);
            Assert.That(playMatchArgs,
                Is.EqualTo(new MatchmakingFinishedData(matchData.MatchId, matchData.MatchDetails!.Map(), matchmakingRoomState.MatchmakingData.QueueName, regionName)));
        }

        [Test]
        public void PlayMatchShouldNotBeCalledAfterReceivingMatchDataIfGameplaySceneIsNotSetToBeLoadedAfterMatchmaking()
        {
            var matchmakingRoomState = InitialRoomState with
            {
                MatchmakingData = InitialRoomState.MatchmakingData! with
                {
                    MatchData = null,
                },
            };
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            var matchData = Defaults.CreateMatchData(matchmakingRoomState.Users.Select(x => x.UserId).ToList());
            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = matchmakingRoomState.MatchmakingData! with
                {
                    State = MatchmakingStateDto.Playing,
                    MatchData = matchData,
                    LastStateUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                },
            };

            _ = MatchLauncherMock.ShouldLoadGameplaySceneAfterMatchmaking.Returns(false);
            MatchmakingFinishedData? playMatchArgs = null;
            MatchLauncherMock.When(x => x.PlayMatch(Arg.Any<MatchmakingFinishedData>()))
                .Do(x => playMatchArgs = x.ArgAt<MatchmakingFinishedData>(0));

            // Act
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            Assert.That(playMatchArgs, Is.Null);
        }

        [Test]
        public void PlayMatchShouldNotBeCalledAfterReceivingMatchDataIfGameplaySceneIsSetToBeLoadedAfterMatchmakingButMatchIsAlreadyRunning()
        {
            var matchmakingRoomState = InitialRoomState with
            {
                MatchmakingData = InitialRoomState.MatchmakingData! with
                {
                    MatchData = null,
                },
            };
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            var matchData = Defaults.CreateMatchData(matchmakingRoomState.Users.Select(x => x.UserId).ToList());
            matchmakingRoomState = matchmakingRoomState with
            {
                LastUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                MatchmakingData = matchmakingRoomState.MatchmakingData! with
                {
                    State = MatchmakingStateDto.Playing,
                    MatchData = matchData,
                    LastStateUpdate = matchmakingRoomState.LastUpdate + TimeSpan.FromSeconds(1),
                },
            };

            _ = MatchLauncherMock.ShouldLoadGameplaySceneAfterMatchmaking.Returns(true);
            _ = MatchLauncherMock.IsCurrentlyInMatch.Returns(true);
            MatchmakingFinishedData? playMatchArgs = null;
            MatchLauncherMock.When(x => x.PlayMatch(Arg.Any<MatchmakingFinishedData>()))
                .Do(x => playMatchArgs = x.ArgAt<MatchmakingFinishedData>(0));

            // Act
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChangedDto>>(matchmakingRoomState);

            Assert.That(playMatchArgs, Is.Null);
        }

        [UnityTest]
        public IEnumerator StartMatchmakingTaskShouldBeResolvedOnlyAfterLocalMatchmakingStateIsUpdated() => UniTask.ToCoroutine(async () =>
        {
            var startMatchmakingCalled = false;

            var readyState = InitialRoomState
                .WithUserTeamSwitched(HostId, 0)
                .WithUserReadinessChanged(HostId, true);
            var matchmakingState = readyState
                .WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingStateDto.Matchmaking));

            EmitRoomUpdate(readyState);

            MatchLauncherMock.When(x => x.StartMatchmaking(Arg.Any<IRoom>()))
                .Do(_ => startMatchmakingCalled = true);
            RoomsClientMock.When(x => x.LeaveRoom(Arg.Any<Guid>()))
                .Do(args => RoomsClientMock.LeftRoom += Raise.Event<Action<LeftRoomArgs>>(new LeftRoomArgs(args.ArgAt<Guid>(0), LeavingReason.UserLeft)));

            var matchmakingTask = RoomsManager.CurrentRoom!.StartMatchmaking();

            await UniTask.WaitUntil(() => startMatchmakingCalled);
            Assert.That(RoomsManager.CurrentRoom!.State.MatchmakingData!.MatchmakingState, Is.EqualTo(MatchmakingState.Unlocked));

            EmitRoomUpdate(matchmakingState);
            await matchmakingTask;
            Assert.That(RoomsManager.CurrentRoom!.State.MatchmakingData!.MatchmakingState, Is.Not.EqualTo(MatchmakingState.Unlocked));
        });
    }
}
