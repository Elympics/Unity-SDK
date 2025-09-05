using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Lobby;
using Elympics.Lobby.Models;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using UnityEngine.TestTools;

#nullable enable

namespace Elympics.Tests.Rooms
{
    [Category("Rooms")]
    internal partial class TestRoomsClient
    {

        [Test]
        public void RoomClientShouldNotBeWatchingRoomsByDefault() =>
            Assert.That(RoomsClient.IsWatchingRooms, Is.False);

        [UnityTest]
        public IEnumerator WatchingRequestShouldResultInExceptionIfUnwatchingOperationIsCurrentlyInProgress() => UniTask.ToCoroutine(async () =>
        {
            var sessionMock = Substitute.For<IWebSocketSessionInternal>();
            RoomsClient.Session = sessionMock;
            _ = sessionMock.ExecuteOperation(null!)
                .ReturnsForAnyArgs(UniTask.FromResult(new OperationResultDto(Guid.Empty)));
            await RoomsClient.WatchRooms();
            sessionMock.ClearSubstitute(ClearOptions.ReturnValues);
            var operationsExecuted = 0;
            _ = sessionMock.ExecuteOperation(null!)
                .ReturnsForAnyArgs(UniTask.Never<OperationResultDto>(CancellationToken.None))
                .AndDoes(_ => operationsExecuted++);
            RoomsClient.UnwatchRooms().Forget();
            await UniTask.WaitUntil(() => operationsExecuted > 0);

            // Act
            var result = await UniTask.Create(async () => await RoomsClient.WatchRooms()).Catch();

            Assert.That(result, Is.InstanceOf<InvalidOperationException>());
            Assert.That(operationsExecuted, Is.EqualTo(1));
            Assert.That(RoomsClient.IsWatchingRooms, Is.False);
        });

        [UnityTest]
        public IEnumerator WatchingRequestShouldWaitForAnotherWatchingRequestAlreadyInProgress() => UniTask.ToCoroutine(async () =>
        {
            var sessionMock = Substitute.For<IWebSocketSessionInternal>();
            RoomsClient.Session = sessionMock;
            var shouldReturn = false;
            var operationsExecuted = 0;
            _ = sessionMock.ExecuteOperation(null!)
                .ReturnsForAnyArgs(UniTask.WaitUntil(() => shouldReturn).ContinueWith(() => new OperationResultDto(Guid.Empty)))
                .AndDoes(_ => operationsExecuted++);
            RoomsClient.WatchRooms().Forget();
            await UniTask.WaitUntil(() => operationsExecuted > 0);

            var awaitable = UniTask.Create(async () => await RoomsClient.WatchRooms());
            shouldReturn = true;

            // Act
            await awaitable;

            Assert.That(operationsExecuted, Is.EqualTo(1));
            Assert.That(RoomsClient.IsWatchingRooms, Is.True);
        });

        [UnityTest]
        public IEnumerator UnwatchingRequestShouldResultInExceptionIfWatchingOperationIsCurrentlyInProgress() => UniTask.ToCoroutine(async () =>
        {
            var sessionMock = Substitute.For<IWebSocketSessionInternal>();
            var operationsExecuted = 0;
            _ = sessionMock.ExecuteOperation(null!)
                .ReturnsForAnyArgs(UniTask.Never<OperationResultDto>(CancellationToken.None))
                .AndDoes(_ => operationsExecuted++);
            RoomsClient.Session = sessionMock;
            RoomsClient.WatchRooms().Forget();
            await UniTask.WaitUntil(() => operationsExecuted > 0);

            // Act
            var result = await UniTask.Create(async () => await RoomsClient.UnwatchRooms()).Catch();

            Assert.That(result, Is.InstanceOf<InvalidOperationException>());
            Assert.That(operationsExecuted, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator UnwatchingRequestShouldWaitForAnotherUnwatchingRequestAlreadyInProgress() => UniTask.ToCoroutine(async () =>
        {
            var sessionMock = Substitute.For<IWebSocketSessionInternal>();
            RoomsClient.Session = sessionMock;
            _ = sessionMock.ExecuteOperation(null!)
                .ReturnsForAnyArgs(UniTask.FromResult(new OperationResultDto(Guid.Empty)));
            await RoomsClient.WatchRooms();
            sessionMock.ClearSubstitute(ClearOptions.ReturnValues);
            var shouldReturn = false;
            var operationsExecuted = 0;
            _ = sessionMock.ExecuteOperation(null!)
                .ReturnsForAnyArgs(UniTask.WaitUntil(() => shouldReturn).ContinueWith(() => new OperationResultDto(Guid.Empty)))
                .AndDoes(_ => operationsExecuted++);
            RoomsClient.UnwatchRooms().Forget();
            await UniTask.WaitUntil(() => operationsExecuted > 0);

            var awaitable = UniTask.Create(async () => await RoomsClient.UnwatchRooms());
            shouldReturn = true;

            // Act
            await awaitable;

            Assert.That(operationsExecuted, Is.EqualTo(1));
            Assert.That(RoomsClient.IsWatchingRooms, Is.False);
        });

        [UnityTest]
        public IEnumerator WatchingRequestShouldBeIgnoredIfRoomsAreAlreadyWatched() => UniTask.ToCoroutine(async () =>
        {
            var sessionMock = Substitute.For<IWebSocketSessionInternal>();
            RoomsClient.Session = sessionMock;
            var operationsExecuted = 0;
            _ = sessionMock.ExecuteOperation(null!)
                .ReturnsForAnyArgs(UniTask.FromResult(new OperationResultDto(Guid.Empty)))
                .AndDoes(_ => operationsExecuted++);
            RoomsClient.WatchRooms().Forget();
            await UniTask.WaitUntil(() => RoomsClient.IsWatchingRooms);

            var awaitable = UniTask.Create(async () => await RoomsClient.WatchRooms());

            // Act
            await awaitable;

            Assert.That(operationsExecuted, Is.EqualTo(1));
            Assert.That(RoomsClient.IsWatchingRooms, Is.True);
        });

        [UnityTest]
        public IEnumerator UnwatchingRequestShouldBeIgnoredIfRoomsAreCurrentlyNotBeingWatched() => UniTask.ToCoroutine(async () =>
        {
            var sessionMock = Substitute.For<IWebSocketSessionInternal>();
            RoomsClient.Session = sessionMock;
            var operationsExecuted = 0;
            _ = sessionMock.ExecuteOperation(null!)
                .ReturnsForAnyArgs(UniTask.FromResult(new OperationResultDto(Guid.Empty)))
                .AndDoes(_ => operationsExecuted++);

            var awaitable = UniTask.Create(async () => await RoomsClient.UnwatchRooms());

            // Act
            await awaitable;

            Assert.That(operationsExecuted, Is.EqualTo(0));
            Assert.That(RoomsClient.IsWatchingRooms, Is.False);
        });
    }
}
