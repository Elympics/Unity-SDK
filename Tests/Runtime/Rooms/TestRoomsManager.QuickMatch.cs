using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Lobby.InternalModels;
using Elympics.Communication.Lobby.InternalModels.FromLobby;
using Elympics.Communication.Rooms.InternalModels;
using Elympics.Communication.Utils;
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
                .WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking, matchedUsers));

            var matchDataState = matchmakingState
                .WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingStateDto.Playing, matchedUsers));

            SetRoomAsTrackedWhenItGetsJoined();
            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()))
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
        public IEnumerator CurrentRoomShouldBeNullAfterQuickMatchIsCancelled_CancellationToken() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            _ = RoomsClientMock.SessionConnectionDetails.Returns(Defaults.CreateConnectionDetails(HostId));
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(RoomId));

            var teamChangedState = InitialRoomState
                .WithUserTeamSwitched(HostId, 0);

            var readyState = teamChangedState
                .WithUserReadinessChanged(HostId, true);

            var matchmakingState = readyState
                .WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingStateDto.Matchmaking));

            SetRoomAsTrackedWhenItGetsJoined();

            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(readyState));
            RoomsClientMock.When(x => x.ChangeTeam(Arg.Any<Guid>(), Arg.Any<uint?>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(teamChangedState));
            MatchLauncherMock.When(x => x.StartMatchmaking(Arg.Any<IRoom>()))
                .Do(_ => EmitRoomUpdate(matchmakingState));
            RoomsClientMock.When(x => x.LeaveRoom(Arg.Any<Guid>()))
                .Do(args => RoomsClientMock.LeftRoom += Raise.Event<Action<LeftRoomArgs>>(new LeftRoomArgs(args.ArgAt<Guid>(0), LeavingReason.UserLeft)));

            using var cts = new CancellationTokenSource();

            // Simulate cancellation after matchmaking starts but before it completes
            var quickMatchTask = RoomsManager.StartQuickMatch("", Array.Empty<byte>(), Array.Empty<float>(), ct: cts.Token);
            await UniTask.DelayFrame(2, cancellationToken: CancellationToken.None);

            // Act
            cts.Cancel();

            // Assert
            var exception = await AssertThrowsAsync<ElympicsException>(quickMatchTask);
            Assert.That(exception.InnerException, Is.InstanceOf<OperationCanceledException>());
            Assert.That(RoomsManager.CurrentRoom, Is.Null, $"{nameof(RoomsManager.CurrentRoom)} should be null after cancellation");
        });

        [UnityTest]
        public IEnumerator CurrentRoomShouldBeNullAfterQuickMatchIsCancelled_CancelMatchmaking() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            _ = RoomsClientMock.SessionConnectionDetails.Returns(Defaults.CreateConnectionDetails(HostId));
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(RoomId));

            var teamChangedState = InitialRoomState
                .WithUserTeamSwitched(HostId, 0);

            var readyState = teamChangedState
                .WithUserReadinessChanged(HostId, true);

            var matchmakingState = readyState
                .WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingStateDto.Matchmaking));

            SetRoomAsTrackedWhenItGetsJoined();

            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(readyState));
            RoomsClientMock.When(x => x.ChangeTeam(Arg.Any<Guid>(), Arg.Any<uint?>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(teamChangedState));
            MatchLauncherMock.When(x => x.StartMatchmaking(Arg.Any<IRoom>()))
                .Do(_ => EmitRoomUpdate(matchmakingState));
            MatchLauncherMock.When(x => x.CancelMatchmaking(Arg.Any<IRoom>(), Arg.Any<CancellationToken>()))
                .Do(args => RoomsClientMock.LeftRoom += Raise.Event<Action<LeftRoomArgs>>(new LeftRoomArgs(args.ArgAt<IRoom>(0).RoomId, LeavingReason.RoomClosed)));
            RoomsClientMock.When(x => x.LeaveRoom(Arg.Any<Guid>()))
                .Do(args => RoomsClientMock.LeftRoom += Raise.Event<Action<LeftRoomArgs>>(new LeftRoomArgs(args.ArgAt<Guid>(0), LeavingReason.UserLeft)));

            // Simulate cancellation after matchmaking starts but before it completes
            var quickMatchTask = RoomsManager.StartQuickMatch("");
            await UniTask.WaitUntil(() => RoomsManager.CurrentRoom != null && RoomsManager.CurrentRoom.State.MatchmakingData?.MatchmakingState != MatchmakingState.Unlocked);

            // Act
            RoomsManager.CurrentRoom?.CancelMatchmaking().Forget();

            // Assert
            var exception = await AssertThrowsAsync<ElympicsException>(quickMatchTask);
            Assert.That(exception.InnerException, Is.InstanceOf<OperationCanceledException>());
            Assert.That(RoomsManager.CurrentRoom, Is.Null, $"{nameof(RoomsManager.CurrentRoom)} should be null after cancellation");
        });

        [UnityTest]
        public IEnumerator LeaveShouldBeSkippedIfRoomCeasesToExistAfterQuickMatchIsCancelled() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            _ = RoomsClientMock.SessionConnectionDetails.Returns(Defaults.CreateConnectionDetails(HostId));
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(RoomId));

            var teamChangedState = InitialRoomState
                .WithUserTeamSwitched(HostId, 0);

            var readyState = teamChangedState
                .WithUserReadinessChanged(HostId, true);

            var matchmakingState = readyState
                .WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingStateDto.Matchmaking));

            SetRoomAsTrackedWhenItGetsJoined();

            var leaveCalled = false;
            using var cts = new CancellationTokenSource();

            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(readyState));
            RoomsClientMock.When(x => x.ChangeTeam(Arg.Any<Guid>(), Arg.Any<uint?>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(teamChangedState));
            MatchLauncherMock.When(x => x.StartMatchmaking(Arg.Any<IRoom>()))
                .Do(_ =>
                {
                    EmitRoomUpdate(matchmakingState);
                    cts.Cancel();
                });
            MatchLauncherMock.When(x => x.CancelMatchmaking(Arg.Any<IRoom>(), Arg.Any<CancellationToken>()))
                .Do(_ => RoomsManager.CurrentRoom?.Dispose());
            RoomsClientMock.When(x => x.LeaveRoom(Arg.Any<Guid>()))
                .Do(_ => leaveCalled = true);

            // Act
            var exception = await AssertThrowsAsync<OperationCanceledException>(RoomsManager.StartQuickMatch("", Array.Empty<byte>(), Array.Empty<float>(), ct: cts.Token));

            // Assert
            Assert.That(exception.CancellationToken, Is.EqualTo(cts.Token));
            Assert.That(leaveCalled, Is.False);
        });

        [UnityTest]
        public IEnumerator QuickMatchLobbyOperationException() => UniTask.ToCoroutine(async () =>
        {
            const string regionName = "test-region";
            var userId = InitialRoomState.Users.First().User.ToPublicModel().UserId;
            var connectionDetails = Defaults.CreateConnectionDetails(userId, regionName);
            _ = RoomsClientMock.SessionConnectionDetails.Returns(connectionDetails);
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(RoomId));

            var teamChangedState = InitialRoomState
                .WithUserTeamSwitched(userId, 0);

            var readyState = teamChangedState
                .WithUserReadinessChanged(userId, true);

            var matchedUsers = new List<Guid> { HostId, Guid.NewGuid() };

            var matchmakingState = readyState
                .WithMatchmakingData(_ => Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking, matchedUsers));

            SetRoomAsTrackedWhenItGetsJoined();
            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()))
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
            Assert.Null(RoomsManager.CurrentRoom);
        });

        [UnityTest]
        public IEnumerator QuickMatchLobbyMatchmakingInitializingFailed() => UniTask.ToCoroutine(async () =>
        {
            const string regionName = "test-region";
            var userId = InitialRoomState.Users.First().User.ToPublicModel().UserId;
            var connectionDetails = Defaults.CreateConnectionDetails(userId, regionName);
            _ = RoomsClientMock.SessionConnectionDetails.Returns(connectionDetails);
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(RoomId));

            var teamChangedState = InitialRoomState
                .WithUserTeamSwitched(userId, 0);

            var readyState = teamChangedState
                .WithUserReadinessChanged(userId, true);

            var matchmakingState = readyState
                .WithMatchmakingData(_ => Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking));

            var matchData = new MatchDataDto(userId, MatchStateDto.InitializingFailed, null, "Test Fail Reason");

            var matchDataState = readyState
                .WithMatchmakingData(_ => Defaults.CreateMatchmakingData(MatchmakingStateDto.Unlocked).WithMatchData(matchData));

            SetRoomAsTrackedWhenItGetsJoined();
            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(readyState));
            RoomsClientMock.When(x => x.ChangeTeam(Arg.Any<Guid>(), Arg.Any<uint?>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(teamChangedState));
            MatchLauncherMock.When(x => x.StartMatchmaking(Arg.Any<IRoom>()))
                .Do(_ => MatchmakingFlow().Forget());
            RoomsClientMock.When(x => x.LeaveRoom(Arg.Any<Guid>()))
                .Do(args => RoomsClientMock.LeftRoom += Raise.Event<Action<LeftRoomArgs>>(new LeftRoomArgs(args.ArgAt<Guid>(0), LeavingReason.UserLeft)));

            // Act
            _ = await AssertThrowsAsync<LobbyOperationException>(async () => await RoomsManager.StartQuickMatch("", Array.Empty<byte>(), Array.Empty<float>()));
            Assert.Null(RoomsManager.CurrentRoom);
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
            var userId = InitialRoomState.Users.First().User.ToPublicModel().UserId;
            var connectionDetails = Defaults.CreateConnectionDetails(userId, regionName);
            _ = RoomsClientMock.SessionConnectionDetails.Returns(connectionDetails);
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(RoomId));

            var teamChangedState = InitialRoomState
                .WithUserTeamSwitched(userId, 0);

            var readyState = teamChangedState
                .WithUserReadinessChanged(userId, true);

            var matchmakingState = readyState
                .WithMatchmakingData(_ => Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking).WithMatchData(null));

            SetRoomAsTrackedWhenItGetsJoined();

            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()))
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
                .WithMatchmakingData(_ => Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking).WithMatchData(null));

            var matchData = new MatchDataDto(Guid.Empty,
                MatchStateDto.Running,
                new MatchDetailsDto(newMatchmakingState.Users.Select(x => x.User.ToPublicModel().UserId).ToList(), string.Empty, string.Empty, string.Empty, new byte[] { }, new float[] { }),
                string.Empty);

            var newMatchDataState = newMatchmakingState
                .WithMatchmakingData(_ => Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking).WithMatchData(matchData));

            SetRoomAsTrackedWhenItGetsJoined(newMatchmakingRoomState);

            MatchLauncherMock.ClearSubstitute(ClearOptions.CallActions);
            MatchLauncherMock.When(x => x.StartMatchmaking(Arg.Any<IRoom>()))
                .Do(_ => MatchmakingFlow().Forget());
            RoomsClientMock.ClearSubstitute(ClearOptions.CallActions);
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(newGuid));
            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()))
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

        [UnityTest]
        public IEnumerator CancellingMatchmakingShouldBeIgnoredCorrectlyIfMatchmakingStateChangesFromMatchmakingToMatchedInTheMeantime() => UniTask.ToCoroutine(async () =>
        {
            ElympicsTimeout.RoomStateChangeConfirmationTimeout = TimeSpan.FromSeconds(15);
            _ = RoomsClientMock.SessionConnectionDetails.Returns(Defaults.CreateConnectionDetails(HostId));
            RoomsClientMock.ReturnsForJoinOrCreate(() => UniTask.FromResult(RoomId));

            var teamChangedState = InitialRoomState
                .WithUserTeamSwitched(HostId, 0);

            var readyState = teamChangedState
                .WithUserReadinessChanged(HostId, true);

            var matchedUsers = new List<Guid> { HostId, Guid.NewGuid() };

            var requestingMatchmakingState = readyState
                .WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingStateDto.RequestingMatchmaking, matchedUsers));

            var matchmakingState = requestingMatchmakingState
                .WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingStateDto.Matchmaking, matchedUsers));

            var matchedState = matchmakingState
                .WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingStateDto.Matched, matchedUsers));

            var matchDataState = matchedState
                .WithMatchmakingData(Defaults.CreateMatchmakingData(MatchmakingStateDto.Playing, matchedUsers));

            using var cts = new CancellationTokenSource();

            SetRoomAsTrackedWhenItGetsJoined();
            RoomsClientMock.When(x => x.SetReady(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<float[]>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(readyState));
            RoomsClientMock.When(x => x.ChangeTeam(Arg.Any<Guid>(), Arg.Any<uint?>(), Arg.Any<CancellationToken>()))
                .Do(_ => EmitRoomUpdate(teamChangedState));
            MatchLauncherMock.When(x => x.StartMatchmaking(Arg.Any<IRoom>()))
                .Do(_ => MatchmakingFlow().Forget());
            MatchLauncherMock.When(x => x.CancelMatchmaking(Arg.Any<IRoom>(), Arg.Any<CancellationToken>()))
                .Do(_ => throw new LobbyOperationException(new OperationResultDto(Guid.Empty, ErrorBlameDto.UserError, ErrorKindDto.RoomAlreadyInMatchedState, "")));

            // Act
            var result = await RoomsManager.StartQuickMatch("", Array.Empty<byte>(), Array.Empty<float>(), ct: cts.Token);

            Assert.That(result, Is.Not.Null);

            async UniTask MatchmakingFlow()
            {
                EmitRoomUpdate(requestingMatchmakingState);
                await GetResponseDelay();
                EmitRoomUpdate(matchmakingState);
                cts.Cancel();
                await GetResponseDelay();
                EmitRoomUpdate(matchedState);
                await GetResponseDelay();
                await GetResponseDelay();
                await GetResponseDelay();
                await GetResponseDelay();
                await GetResponseDelay();
                await GetResponseDelay();
                await GetResponseDelay();
                await GetResponseDelay();
                EmitRoomUpdate(matchDataState);
            }
        });
    }
}
