using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Rooms.Models;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using UnityEngine.TestTools;
using static Elympics.Tests.Common.AsyncAsserts;
using MatchmakingState = Elympics.Rooms.Models.MatchmakingState;

#nullable enable

namespace Elympics.Tests.Rooms
{
    [Category("Rooms")]
    internal class TestRoomsManager_QuickMatch : TestRoomsManager
    {
        [UnityTest]
        public IEnumerator HappyPathStartingQuickMatchShouldSucceed() => UniTask.ToCoroutine(async () =>
        {
            _ = RoomsClientMock.SessionConnectionDetails.Returns(Defaults.CreateConnectionDetails(HostId));
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(RoomId));

            var teamChangedState = InitialRoomState
                .WithUserTeamSwitched(HostId, 0);

            var readyState = teamChangedState
                .WithUserReadinessChanged(HostId, true);

            var matchedUsers = new List<Guid> { HostId, Guid.NewGuid() };

            var matchmakingState = readyState
                .WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingState.RequestingMatchmaking, matchedUsers));

            var matchDataState = matchmakingState
                .WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingState.Playing, matchedUsers));

            SetRoomAsTrackedWhenItGetsJoined();
            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(readyState));
            RoomsClientMock.When(x => x.ChangeTeam(Arg.Any<Guid>(), Arg.Any<uint?>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(teamChangedState));
            MatchLauncherMock.When(x => x.StartMatchmaking(Arg.Any<IRoom>()))
                .Do(_ => MatchmakingFlow().Forget());

            // Act
            _ = await RoomsManager.StartQuickMatch("", Array.Empty<byte>(), Array.Empty<float>());

            async UniTask MatchmakingFlow()
            {
                EmitRoomUpdate(matchmakingState);
                await GetResponseDelay();
                EmitRoomUpdate(matchDataState);
            }
        });

        [UnityTest]
        public IEnumerator QuickMatchLobbyOperationException() => UniTask.ToCoroutine(async () =>
        {
            const string regionName = "test-region";
            var userId = InitialRoomState.Users.First().UserId;
            var connectionDetails = Defaults.CreateConnectionDetails(userId, regionName);
            _ = RoomsClientMock.SessionConnectionDetails.Returns(connectionDetails);
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(RoomId));

            var teamChangedState = InitialRoomState
                .WithUserTeamSwitched(userId, 0);

            var readyState = teamChangedState
                .WithUserReadinessChanged(userId, true);

            var matchedUsers = new List<Guid> { HostId, Guid.NewGuid() };

            var matchmakingState = readyState
                .WithMatchmakingData(_ => Defaults.CreateMatchmakingData(MatchmakingState.RequestingMatchmaking, matchedUsers));

            SetRoomAsTrackedWhenItGetsJoined();
            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(readyState));
            RoomsClientMock.When(x => x.ChangeTeam(Arg.Any<Guid>(), Arg.Any<uint?>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(teamChangedState));
            MatchLauncherMock.When(x => x.StartMatchmaking(Arg.Any<IRoom>()))
                .Do(_ =>
                {
                    EmitRoomUpdate(matchmakingState);
                    throw new LobbyOperationException("Test exception: timed out.");
                });
            RoomsClientMock.When(x => x.LeaveRoom(Arg.Any<Guid>()))
                .Do(args => RoomsClientMock.LeftRoom += Raise.Event<Action<LeftRoomArgs>>(new LeftRoomArgs(args.ArgAt<Guid>(0), LeavingReason.UserLeft)));

            // Act
            _ = await AssertThrowsAsync<LobbyOperationException>(async () => await RoomsManager.StartQuickMatch("", Array.Empty<byte>(), Array.Empty<float>()));
            Assert.AreEqual(0, RoomsManager.ListJoinedRooms().Count);
        });

        [UnityTest]
        public IEnumerator QuickMatchLobbyMatchmakingInitializingFailed() => UniTask.ToCoroutine(async () =>
        {
            const string regionName = "test-region";
            var userId = InitialRoomState.Users.First().UserId;
            var connectionDetails = Defaults.CreateConnectionDetails(userId, regionName);
            _ = RoomsClientMock.SessionConnectionDetails.Returns(connectionDetails);
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(RoomId));

            var teamChangedState = InitialRoomState
                .WithUserTeamSwitched(userId, 0);

            var readyState = teamChangedState
                .WithUserReadinessChanged(userId, true);

            var matchmakingState = readyState
                .WithMatchmakingData(_ => Defaults.CreateMatchmakingData(MatchmakingState.RequestingMatchmaking));

            var matchData = new MatchData(userId, MatchState.InitializingFailed, null, "Test Fail Reason");

            var matchDataState = readyState
                .WithMatchmakingData(_ => Defaults.CreateMatchmakingData(MatchmakingState.Unlocked).WithMatchData(matchData));

            SetRoomAsTrackedWhenItGetsJoined();
            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(readyState));
            RoomsClientMock.When(x => x.ChangeTeam(Arg.Any<Guid>(), Arg.Any<uint?>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(teamChangedState));
            MatchLauncherMock.When(x => x.StartMatchmaking(Arg.Any<IRoom>()))
                .Do(_ => MatchmakingFlow().Forget());
            RoomsClientMock.When(x => x.LeaveRoom(Arg.Any<Guid>()))
                .Do(args => RoomsClientMock.LeftRoom += Raise.Event<Action<LeftRoomArgs>>(new LeftRoomArgs(args.ArgAt<Guid>(0), LeavingReason.UserLeft)));

            // Act
            _ = await AssertThrowsAsync<LobbyOperationException>(async () => await RoomsManager.StartQuickMatch("", Array.Empty<byte>(), Array.Empty<float>()));
            Assert.AreEqual(0, RoomsManager.ListJoinedRooms().Count);
            return;

            async UniTask MatchmakingFlow()
            {
                EmitRoomUpdate(matchmakingState);
                await GetResponseDelay();
                EmitRoomUpdate(matchDataState);
            }
        });

        [UnityTest]
        public IEnumerator CanQuickMatchAgainAfterTimeoutException() => UniTask.ToCoroutine(async () =>
        {
            const string regionName = "test-region";
            var userId = InitialRoomState.Users.First().UserId;
            var connectionDetails = Defaults.CreateConnectionDetails(userId, regionName);
            _ = RoomsClientMock.SessionConnectionDetails.Returns(connectionDetails);
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(RoomId));

            var teamChangedState = InitialRoomState
                .WithUserTeamSwitched(userId, 0);

            var readyState = teamChangedState
                .WithUserReadinessChanged(userId, true);

            var matchmakingState = readyState
                .WithMatchmakingData(_ => Defaults.CreateMatchmakingData(MatchmakingState.RequestingMatchmaking).WithMatchData(null));

            SetRoomAsTrackedWhenItGetsJoined();

            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(readyState));
            RoomsClientMock.When(x => x.ChangeTeam(Arg.Any<Guid>(), Arg.Any<uint?>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(teamChangedState));
            MatchLauncherMock.When(x => x.StartMatchmaking(Arg.Any<IRoom>()))
                .Do(_ =>
                {
                    EmitRoomUpdate(matchmakingState);
                    throw new LobbyOperationException("Test exception: timed out.");
                });
            RoomsClientMock.When(x => x.LeaveRoom(Arg.Any<Guid>()))
                .Do(args => RoomsClientMock.LeftRoom += Raise.Event<Action<LeftRoomArgs>>(new LeftRoomArgs(args.ArgAt<Guid>(0), LeavingReason.UserLeft)));

            // Act
            _ = await AssertThrowsAsync<LobbyOperationException>(async () => await RoomsManager.StartQuickMatch("", Array.Empty<byte>(), Array.Empty<float>()));

            Guid newGuid = new("10100000000000000000000000000002");
            var newMatchmakingRoomState = Defaults.CreateRoomState(newGuid, HostId);

            var newTeamChangedState = newMatchmakingRoomState
                .WithUserTeamSwitched(userId, 0);

            var newReadyState = newTeamChangedState
                .WithUserReadinessChanged(userId, true);

            var newMatchmakingState = newReadyState
                .WithMatchmakingData(_ => Defaults.CreateMatchmakingData(MatchmakingState.RequestingMatchmaking).WithMatchData(null));

            var matchData = new MatchData(Guid.Empty,
                MatchState.Running,
                new MatchDetails(newMatchmakingState.Users.Select(x => x.UserId).ToList(), string.Empty, string.Empty, string.Empty, new byte[] { }, new float[] { }),
                string.Empty);

            var newMatchDataState = newMatchmakingState
                .WithMatchmakingData(_ => Defaults.CreateMatchmakingData(MatchmakingState.RequestingMatchmaking).WithMatchData(matchData));

            SetRoomAsTrackedWhenItGetsJoined(newMatchmakingRoomState);

            MatchLauncherMock.ClearSubstitute(ClearOptions.CallActions);
            MatchLauncherMock.When(x => x.StartMatchmaking(Arg.Any<IRoom>()))
                .Do(_ => MatchmakingFlow().Forget());
            RoomsClientMock.ClearSubstitute(ClearOptions.CallActions);
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(newGuid));
            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(newReadyState));
            RoomsClientMock.When(x => x.ChangeTeam(Arg.Any<Guid>(), Arg.Any<uint?>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(newTeamChangedState));

            // Act
            _ = await RoomsManager.StartQuickMatch("", Array.Empty<byte>(), Array.Empty<float>());

            async UniTask MatchmakingFlow()
            {
                EmitRoomUpdate(newMatchmakingState);
                await GetResponseDelay();
                EmitRoomUpdate(newMatchDataState);
            }
        });
    }
}
