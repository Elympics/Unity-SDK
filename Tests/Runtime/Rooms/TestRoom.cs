using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Rooms.PublicModels;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Models.Matchmaking;
using Elympics.Rooms.Models;
using Elympics.Tests.Common.RoomMocks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Elympics.Tests.Common.AsyncAsserts;
using MatchmakingState = Elympics.Rooms.Models.MatchmakingState;

#nullable enable

namespace Elympics.Tests.Rooms
{
    [Category("Rooms")]
    public class TestRoom
    {
        private const string RegionName = "test-region";

        private static readonly Guid RoomId = new("10100000000000000000000000aaaa01");
        private static readonly Guid HostId = new("10100000000000000000000000bbbb01");
        private static readonly SessionConnectionDetails ConnectionDetails = Defaults.CreateConnectionDetails(HostId, RegionName);

        private static RoomStateChanged InitialRoomState => Defaults.CreateRoomState(RoomId, HostId);

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
            var room = new Room(null!, null!, RoomId, InitialRoomState);

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
            var room = new Room(null!, null!, RoomId, InitialRoomState);

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
            var room = new Room(null!,
                null!,
                RoomId,
                InitialRoomState with
                {
                    HasPrivilegedHost = false,
                },
                true);

            _ = await AssertThrowsAsync<RoomPrivilegeException>(async () => await room.UpdateRoomParams());
        });

        [UnityTest]
        public IEnumerator TestRoomWithoutMatchmakingFunctionality() => UniTask.ToCoroutine(async () =>
        {
            var room = new Room(null!,
                null!,
                RoomId,
                InitialRoomState with
                {
                    MatchmakingData = null,
                },
                true);

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
            var room = new Room(null!, null!, RoomId, InitialRoomState, true);

            await room.UpdateRoomParams();

            LogAssert.Expect(LogType.Warning, new Regex("No change compared to current room parameters."));
        });

        private readonly byte[] _keyInLimit = Enumerable.Repeat((byte)0x4b, 512).ToArray();
        private readonly byte[] _valueOverTheLimit = Enumerable.Repeat((byte)0x56, 513).ToArray();
        private readonly byte[] _valueInLimit = Enumerable.Repeat((byte)0x56, 512).ToArray();

        [UnityTest]
        public IEnumerator TestUpdateRoomParamsWithCustomRoomDataExceedingMaxSizeLimit() => UniTask.ToCoroutine(async () =>
        {
            var room = new Room(null!, null!, RoomId, InitialRoomState, true);
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
            var room = new Room(null!, new RoomClientMock(), RoomId, InitialRoomState, true);
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
            IRoom room = new Room(null!, null!, RoomId, InitialRoomState, true);
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
            IRoom room = new Room(null!, new RoomClientMock(), RoomId, InitialRoomState, true);
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
            var room = new Room(null!,
                null!,
                RoomId,
                InitialRoomState with
                {
                    RoomName = roomName,
                    IsPrivate = isPrivate,
                    CustomData = roomCustomData,
                },
                true);

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
            var room = new Room(null!,
                null!,
                RoomId,
                InitialRoomState with
                {
                    RoomName = roomName,
                    IsPrivate = isPrivate,
                    MatchmakingData = InitialRoomState.MatchmakingData! with
                    {
                        CustomData = customMatchmakingData,
                    },
                },
                true);
            await room.UpdateRoomParams(roomName, isPrivate, null, customMatchmakingData);
            LogAssert.Expect(LogType.Warning, new Regex("No change compared to current room parameters."));
        });

        [Test]
        public void PlayingAvailableMatchShouldSucceedIfMatchDetailsAreAvailable()
        {
            var matchLauncher = new MatchLauncherMock { IsCurrentlyInMatch = false };
            var roomsClient = new RoomClientMock();
            roomsClient.SetSessionConnectionDetails(ConnectionDetails);

            var roomState = Defaults.CreateRoomState(RoomId, HostId, mmState: MatchmakingState.Playing);
            var room = new Room(matchLauncher, roomsClient, RoomId, roomState, true);
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

            var roomState = Defaults.CreateRoomState(RoomId, HostId, mmState: MatchmakingState.Playing);
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
            var room = new Room(matchLauncher, roomsClient, RoomId, roomStateWithoutMatchDetails, true);

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

            var roomState = Defaults.CreateRoomState(RoomId, HostId, mmState: MatchmakingState.Playing);
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
            var room = new Room(matchLauncher, roomsClient, RoomId, roomStateWithoutMatchDetails, true);

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

            var roomState = Defaults.CreateRoomState(RoomId, HostId, mmState: MatchmakingState.Playing) with
            {
                MatchmakingData = InitialRoomState.MatchmakingData! with
                {
                    State = MatchmakingState.Matchmaking,
                },
            };
            var room = new Room(matchLauncher, roomsClient, RoomId, roomState, true);

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

            var roomState = Defaults.CreateRoomState(RoomId, HostId, mmState: MatchmakingState.Playing) with
            {
                MatchmakingData = InitialRoomState.MatchmakingData! with
                {
                    State = MatchmakingState.Playing,
                    MatchData = null,
                },
            };
            var room = new Room(matchLauncher, roomsClient, RoomId, roomState, true);

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
            var room = new Room(matchLauncher, roomsClient, RoomId, roomState, true);

            // Act
            TestDelegate testDelegate = () => room.PlayAvailableMatch();

            Assert.That(testDelegate, Throws.Exception.TypeOf<MatchmakingException>().With.Message.Contain("non-matchmaking"));
            Assert.That(matchLauncher.PlayMatchCalledArgs, Is.Null);
        }

        [UnityTest]
        public IEnumerator TestUserLeftRoomAwaitsUntilLeftRoomEventArrived() => UniTask.ToCoroutine(async () =>
        {
            var roomClientMock = Substitute.For<IRoomsClient>();
            roomClientMock.When(x => x.LeaveRoom(RoomId)).Do(x =>
            {
                UniTask.RunOnThreadPool(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(0.5));
                    roomClientMock.LeftRoom += Raise.Event<Action<LeftRoomArgs>>(new LeftRoomArgs(RoomId, LeavingReason.UserLeft));
                }).Forget();
            });
            _ = roomClientMock.CreateRoom(Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CompetitivenessConfig?>(),
                Arg.Any<CancellationToken>()).Returns(info =>
            {
                UniTask.RunOnThreadPool(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(0.5));
                    roomClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChanged>>(Defaults.CreateRoomState(RoomId, HostId));
                }).Forget();
                return UniTask.FromResult(RoomId);
            });
            var roomsManager = new RoomsManager(null!, roomClientMock, new ElympicsLoggerContext(Guid.Empty), null);
            var room = await roomsManager.CreateAndJoinRoom("roonMane", "testQueue", true, false);
            await room.Leave();
            Assert.IsFalse(room.IsJoined);
        });

        [UnityTest]
        public IEnumerator TestUserLeftRoomAwaitsUntilLeftRoomEventArrivedAndRoomWasClosed() => UniTask.ToCoroutine(async () =>
        {
            var roomClientMock = Substitute.For<IRoomsClient>();
            roomClientMock.When(x => x.LeaveRoom(RoomId)).Do(x => roomClientMock.LeftRoom += Raise.Event<Action<LeftRoomArgs>>(new LeftRoomArgs(RoomId, LeavingReason.RoomClosed)));
            _ = roomClientMock.CreateRoom(Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CompetitivenessConfig?>(),
                Arg.Any<CancellationToken>()).Returns(info =>
            {
                UniTask.RunOnThreadPool(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(0.5));
                    roomClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChanged>>(Defaults.CreateRoomState(RoomId, HostId));
                }).Forget();
                return UniTask.FromResult(RoomId);
            });
            var roomsManager = new RoomsManager(null!, roomClientMock, new ElympicsLoggerContext(Guid.Empty), null);
            var room = await roomsManager.CreateAndJoinRoom("roonMane", "testQueue", true, false);
            await room.Leave();
            Assert.IsTrue(room.IsDisposed);
        });

        private static List<(string Name, Func<IRoom, UniTask> Operation, RoomStateChanged RoomState)> cancellingRoomOperationsTestCases = new()
        {
            (nameof(IRoom.ChangeTeam), r => r.ChangeTeam(null), InitialRoomState with { Users = new[] { new UserInfo(HostId, 0, false, null, null) }, MatchmakingData = Defaults.CreateMatchmakingData(MatchmakingState.Matchmaking) }),
            (nameof(IRoom.MarkYourselfReady), r => r.MarkYourselfReady(), InitialRoomState with { Users = new[] { new UserInfo(HostId, 0, false, null, null) } }),
            (nameof(IRoom.MarkYourselfUnready), r => r.MarkYourselfUnready(), InitialRoomState with { Users = new[] { new UserInfo(HostId, 0, true, null, null) } }),
            (nameof(IRoom.StartMatchmaking), r => r.StartMatchmaking(), InitialRoomState with { Users = new[] { new UserInfo(HostId, 0, true, null, null) } }),
            (nameof(IRoom.CancelMatchmaking), r => r.CancelMatchmaking(), InitialRoomState with { Users = new[] { new UserInfo(HostId, 0, true, null, null) }, MatchmakingData = Defaults.CreateMatchmakingData(MatchmakingState.Matchmaking) }),
        };

        [UnityTest]
        public IEnumerator SettingIsJoinedShouldCancelOperationsInProgress(
            [ValueSource(nameof(cancellingRoomOperationsTestCases))]
            (string _, Func<IRoom, UniTask> Operation, RoomStateChanged RoomState) testCase) => UniTask.ToCoroutine(async () =>
        {
            var roomClientMock = Substitute.For<IRoomsClient>();
            _ = roomClientMock.ChangeTeam(default, null)
                .ReturnsForAnyArgs(UniTask.CompletedTask);
            _ = roomClientMock.SetReady(default, null!, null!, default)
                .ReturnsForAnyArgs(UniTask.CompletedTask);
            _ = roomClientMock.SetUnready(default)
                .ReturnsForAnyArgs(UniTask.CompletedTask);
            _ = roomClientMock.StartMatchmaking(default, default)
                .ReturnsForAnyArgs(UniTask.CompletedTask);
            _ = roomClientMock.CancelMatchmaking(default)
                .ReturnsForAnyArgs(UniTask.CompletedTask);

            var matchLauncherMock = new MatchLauncherMock();

            var room = new Room(matchLauncherMock, roomClientMock, RoomId, testCase.RoomState, true);
            var operation = testCase.Operation(room);

            // Act
            ((IRoom)room).IsJoined = false;

            // Assert
            Assert.That(await operation.SuppressCancellationThrow(), Is.True);
        });

        [UnityTest]
        public IEnumerator DisposingRoomShouldCancelOperationsInProgress(
            [ValueSource(nameof(cancellingRoomOperationsTestCases))]
            (string _, Func<IRoom, UniTask> Operation, RoomStateChanged RoomState) testCase) => UniTask.ToCoroutine(async () =>
        {
            var roomClientMock = Substitute.For<IRoomsClient>();
            _ = roomClientMock.ChangeTeam(default, null)
                .ReturnsForAnyArgs(UniTask.CompletedTask);
            _ = roomClientMock.SetReady(default, null!, null!, default)
                .ReturnsForAnyArgs(UniTask.CompletedTask);
            _ = roomClientMock.SetUnready(default)
                .ReturnsForAnyArgs(UniTask.CompletedTask);
            _ = roomClientMock.StartMatchmaking(default, default)
                .ReturnsForAnyArgs(UniTask.CompletedTask);
            _ = roomClientMock.CancelMatchmaking(default)
                .ReturnsForAnyArgs(UniTask.CompletedTask);

            var matchLauncherMock = new MatchLauncherMock();

            var room = new Room(matchLauncherMock, roomClientMock, RoomId, testCase.RoomState, true);
            var operation = testCase.Operation(room);

            // Act
            room.Dispose();

            // Assert
            Assert.That(await operation.SuppressCancellationThrow(), Is.True);
        });

        [UnityTest]
        public IEnumerator LeavingRoomShouldCancelOperationsInProgress(
            [ValueSource(nameof(cancellingRoomOperationsTestCases))]
            (string _, Func<IRoom, UniTask> Operation, RoomStateChanged RoomState) testCase) => UniTask.ToCoroutine(async () =>
        {
            var roomClientMock = Substitute.For<IRoomsClient>();
            _ = roomClientMock.ChangeTeam(default, null)
                .ReturnsForAnyArgs(UniTask.CompletedTask);
            _ = roomClientMock.SetReady(default, null!, null!, default)
                .ReturnsForAnyArgs(UniTask.CompletedTask);
            _ = roomClientMock.SetUnready(default)
                .ReturnsForAnyArgs(UniTask.CompletedTask);
            _ = roomClientMock.StartMatchmaking(default, default)
                .ReturnsForAnyArgs(UniTask.CompletedTask);
            _ = roomClientMock.CancelMatchmaking(default)
                .ReturnsForAnyArgs(UniTask.CompletedTask);

            var matchLauncherMock = new MatchLauncherMock();

            var room = new Room(matchLauncherMock, roomClientMock, RoomId, testCase.RoomState, true);
            roomClientMock.When(x => x.LeaveRoom(Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
                .Do(_ => ((IRoom)room).IsJoined = false);
            var operation = testCase.Operation(room);

            // Act
            await room.Leave();

            // Assert
            Assert.That(await operation.SuppressCancellationThrow(), Is.True);
        });
    }
}
