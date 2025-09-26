using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Lobby.InternalModels;
using Elympics.Communication.Lobby.InternalModels.FromLobby;
using Elympics.Communication.Lobby.InternalModels.ToLobby;
using Elympics.Communication.Rooms.InternalModels;
using Elympics.Communication.Rooms.InternalModels.FromRooms;
using Elympics.Communication.Rooms.InternalModels.ToRooms;
using Elympics.Communication.Utils;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Lobby.Serializers;
using Elympics.Models.Authentication;
using HybridWebSocket;
using MessagePack;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Elympics.Tests.Common.AsyncAsserts;

#nullable enable

namespace Elympics.Tests
{
    [Category("WebSocket")]
    public class TestWebSocketSession
    {
        private static readonly IAsyncEventsDispatcher Dispatcher = AsyncEventsDispatcherMockSetup.CreateMockAsyncEventsDispatcher();
        private static readonly AuthData AuthData = new(new Guid("10000000000000000000000000000001"), "Nickname_10000000000000000000000000000001", string.Empty);
        private static readonly SessionConnectionDetails ConnectionDetails = new("url", AuthData, default, string.Empty, string.Empty);
        private static readonly IWebSocket WebSocketMock = Substitute.For<IWebSocket>();
        private static readonly ILobbySerializer LobbySerializer = Substitute.For<ILobbySerializer>();

        private static readonly TimeSpan DefaultOpeningTimeout = ElympicsTimeout.WebSocketOpeningTimeout;
        private static readonly TimeSpan DefaultOperationTimeout = ElympicsTimeout.WebSocketOperationTimeout;

        private static CancellationTokenSource cts = new();
        private record UnknownMessage : IFromLobby, IToLobby;
        private record UnknownOperation : LobbyOperation;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _ = LobbySerializer.Serialize(Arg.Any<IToLobby>()).Returns(x =>
            {
                var toLobby = (IToLobby)x[0];
                return MessagePackSerializer.Serialize(toLobby);
            });

            _ = LobbySerializer.Deserialize(Arg.Any<byte[]>()).Returns(x =>
            {
                var byteData = (byte[])x[0];
                return MessagePackSerializer.Deserialize<IFromLobby>(byteData);
            });
        }

        [SetUp]
        public void ResetMocks() => cts = new();

        [UnityTest]
        public IEnumerator HappyPathConnectionScenarioShouldSucceed() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateDefaultWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);
        });

        private static WebSocketSession CreateDefaultWebSocketSession()
        {
            _ = WebSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, AuthData.UserId, AuthData.Nickname, null);
            var session = new WebSocketSession(Substitute.For<IWebSocketSessionController>(), Dispatcher, new ElympicsLoggerContext(Guid.Empty), (_, _) => WebSocketMock, LobbySerializer);
            return session;
        }

        private static async UniTask ConnectWebSocketSessionAndAssert(WebSocketSession session)
        {
            var connectedCalled = false;
            session.Connected += SetConnected;
            _ = await session.Connect(ConnectionDetails);
            session.Connected -= SetConnected;

            Assert.True(connectedCalled);
            Assert.True(session.IsConnected);
            void SetConnected() => connectedCalled = true;
        }

        private static void DisconnectWebSocketSessionAndAssert(WebSocketSession session)
        {
            var disconnectedCalled = false;

            session.Disconnected += SetDisconnected;
            session.Disconnect(DisconnectionReason.Closed);
            session.Disconnected -= SetDisconnected;

            Assert.True(disconnectedCalled);
            Assert.False(session.IsConnected);

            void SetDisconnected(DisconnectionData data) => disconnectedCalled = true;
        }

        [UnityTest]
        public IEnumerator WebSocketSessionShouldAllowForReconnecting() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateDefaultWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);
            DisconnectWebSocketSessionAndAssert(session);
            await ConnectWebSocketSessionAndAssert(session);
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenTokenIsCanceledBeforeWsOpens() => UniTask.ToCoroutine(async () =>
        {

            using var cts = new CancellationTokenSource();
            WebSocketMock.ClearSubstitute();
            WebSocketMock.When(x => x.Connect()).Do(_ => cts.Cancel());
            var session = new WebSocketSession(Substitute.For<IWebSocketSessionController>(), Dispatcher, new ElympicsLoggerContext(Guid.Empty), (_, _) => WebSocketMock, LobbySerializer);
            var canceled = await session.Connect(ConnectionDetails, cts.Token).SuppressCancellationThrow();

            Assert.True(canceled.IsCanceled);
            Assert.False(session.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenTokenIsCanceledBeforeWsReceivesResponse() => UniTask.ToCoroutine(async () =>
        {
            using var cts = new CancellationTokenSource();
            _ = WebSocketMock.SetupOpenCloseDefaultBehaviour();
            WebSocketMock.When(x => x.Send(Arg.Any<byte[]>())).Do(_ => cts.Cancel());
            var session = new WebSocketSession(Substitute.For<IWebSocketSessionController>(), Dispatcher, new ElympicsLoggerContext(Guid.Empty), (_, _) => WebSocketMock, LobbySerializer);
            _ = await AssertThrowsAsync<OperationCanceledException>(async () => await session.Connect(ConnectionDetails, cts.Token));

            Assert.False(session.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenThereIsAnErrorBeforeWsOpens() => UniTask.ToCoroutine(async () =>
        {
            const string errorMessage = "test error message";
            _ = WebSocketMock.SetupErrorOnConnectBehaviour(errorMessage).SetupJoinLobby(false, AuthData.UserId, AuthData.Nickname, null);
            using var session = new WebSocketSession(Substitute.For<IWebSocketSessionController>(), Dispatcher, new ElympicsLoggerContext(Guid.Empty), (_, _) => WebSocketMock, LobbySerializer);

            _ = await AssertThrowsAsync<LobbyOperationException>(async () => await session.Connect(ConnectionDetails).SuppressCancellationThrow());
            LogAssert.Expect(LogType.Error, new Regex($".*{errorMessage}.*"));
            Assert.False(session.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenThereIsAnErrorBeforeWsReceivesResponse() => UniTask.ToCoroutine(async () =>
        {
            const string errorMessage = "test error message";
            _ = WebSocketMock.SetupOpenCloseDefaultBehaviour().SetupOnErrorJoinLobby(errorMessage);
            using var session = new WebSocketSession(Substitute.For<IWebSocketSessionController>(), Dispatcher, new ElympicsLoggerContext(Guid.Empty), (_, _) => WebSocketMock, LobbySerializer);

            _ = await AssertThrowsAsync<LobbyOperationException>(async () => await session.Connect(ConnectionDetails).SuppressCancellationThrow());
            LogAssert.Expect(LogType.Error, new Regex($".*{errorMessage}.*"));
            Assert.False(session.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenThereIsAnErrorAfterConnecting() => UniTask.ToCoroutine(async () =>
        {
            const string errorMessage = "test error message";
            using var session = CreateDefaultWebSocketSession();
            await ConnectWebSocketSessionAndAssert(session);

            WebSocketMock.OnError += Raise.Event<WebSocketErrorEventHandler>(errorMessage);
            LogAssert.Expect(LogType.Error, new Regex($".*{errorMessage}.*"));
            Assert.False(session.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenWsClosesBeforeItOpens() => UniTask.ToCoroutine(async () =>
        {
            const string errorMessage = "test error message";
            _ = WebSocketMock.SetupCloseOnConnectBehaviour(errorMessage);
            using var session = new WebSocketSession(Substitute.For<IWebSocketSessionController>(), Dispatcher, new ElympicsLoggerContext(Guid.Empty), (_, _) => WebSocketMock, LobbySerializer);
            _ = await AssertThrowsAsync<LobbyOperationException>(async () => await session.Connect(ConnectionDetails).SuppressCancellationThrow());
            LogAssert.Expect(LogType.Error, new Regex($".*{errorMessage}.*"));
            Assert.False(session.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenWsClosesBeforeItReceivesResponse() => UniTask.ToCoroutine(async () =>
        {
            const string errorMessage = "test error message";
            _ = WebSocketMock.SetupOpenCloseDefaultBehaviour().SetupOnCloseJoinLobby(errorMessage);
            using var session = new WebSocketSession(Substitute.For<IWebSocketSessionController>(), Dispatcher, new ElympicsLoggerContext(Guid.Empty), (_, _) => WebSocketMock, LobbySerializer);

            _ = await AssertThrowsAsync<LobbyOperationException>(async () => await session.Connect(ConnectionDetails).SuppressCancellationThrow());
            LogAssert.Expect(LogType.Error, new Regex($".*{errorMessage}.*"));
            Assert.False(session.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenOpeningWsTakesTooLong() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateDefaultWebSocketSession();
            ElympicsTimeout.WebSocketOpeningTimeout = TimeSpan.Zero;
            _ = await AssertThrowsAsync<LobbyOperationException>(async () => await session.Connect(ConnectionDetails));
            Assert.False(session.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenReceivingResponseTakesTooLong() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateDefaultWebSocketSession();
            ElympicsTimeout.WebSocketOperationTimeout = TimeSpan.Zero;

            _ = await AssertThrowsAsync<LobbyOperationException>(async () => await session.Connect(ConnectionDetails));
            Assert.False(session.IsConnected);
        });

        [UnityTest]
        public IEnumerator PingMessageShouldNotBePassed() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateDefaultWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);

            IFromLobby? passedMessage = null;
            IFromLobby ping = new PingDto();
            var data = MessagePackSerializer.Serialize(ping);
            session.MessageReceived += ReceiveMessage;
            WebSocketMock.OnMessage += Raise.Event<WebSocketMessageEventHandler>(data);
            Assert.IsNull(passedMessage);
            void ReceiveMessage(IFromLobby message) => passedMessage = message;
        });

        [UnityTest]
        public IEnumerator OperationResultMessageShouldNotBePassed() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateDefaultWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);

            IFromLobby? passedMessage = null;
            session.MessageReceived += ReceiveMessage;
            IFromLobby pingData = new PingDto();
            var binaryPing = MessagePackSerializer.Serialize(pingData);
            WebSocketMock.OnMessage += Raise.Event<WebSocketMessageEventHandler>(binaryPing);
            Assert.IsNull(passedMessage);

            void ReceiveMessage(IFromLobby message) => passedMessage = message;
        });

        [UnityTest]
        public IEnumerator OtherMessageTypesShouldNotBePassed() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateDefaultWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);

            IFromLobby? passedMessage = null;
            session.MessageReceived += ReceiveMessage;
            IFromLobby unknownMessage = new UnknownMessage();
            var binary = MessagePackSerializer.Serialize(unknownMessage);
            LogAssert.Expect(LogType.Exception, new Regex($".*Invalid message received.*"));
            WebSocketMock.OnMessage += Raise.Event<WebSocketMessageEventHandler>(binary);
            Assert.IsNull(passedMessage);

            void ReceiveMessage(IFromLobby message) => passedMessage = message;
        });

        [UnityTest]
        public IEnumerator MessageShouldBeReceivedOnceAfterReconnecting() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateDefaultWebSocketSession();
            var counter = 0;
            await ConnectWebSocketSessionAndAssert(session);
            DisconnectWebSocketSessionAndAssert(session);
            await ConnectWebSocketSessionAndAssert(session);

            session.MessageReceived += ReceiveMessage;
            IFromLobby passedMessage = new RoomWasLeftDto(Guid.Empty, LeavingReasonDto.RoomClosed);
            var data = MessagePackSerializer.Serialize(passedMessage);
            WebSocketMock.OnMessage += Raise.Event<WebSocketMessageEventHandler>(data);

            void ReceiveMessage(IFromLobby message) => counter++;
            Assert.AreEqual(1, counter);
        });

        [UnityTest]
        public IEnumerator OperationShouldNotBePossibleToExecuteBeforeConnecting() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateDefaultWebSocketSession();
            var operation = new UnknownOperation();
            WebSocketMock.When(x => x.Send(Arg.Any<byte[]>())).Do(x => Assert.Fail("Operation has been sent."));
            _ = await AssertThrowsAsync<ElympicsException>(UniTask.Create(async () => await session.ExecuteOperation(operation)));
        });

        [UnityTest]
        public IEnumerator OperationShouldBeAbortedWhenReceivingResponseTakesTooLong() => UniTask.ToCoroutine(async () =>
        {
            var operation = new UnknownOperation();
            using var session = CreateDefaultWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);

            ElympicsTimeout.WebSocketOperationTimeout = TimeSpan.Zero;
            _ = await AssertThrowsAsync<LobbyOperationException>(async () => await session.ExecuteOperation(operation));

        });

        [UnityTest]
        public IEnumerator OperationShouldBeCancellable() => UniTask.ToCoroutine(async () =>
        {
            var operation = new UnknownOperation();
            using var session = CreateDefaultWebSocketSession();
            using var cts = new CancellationTokenSource();

            WebSocketMock.When(x => x.Send(Arg.Any<byte[]>())).Do(x => cts.Cancel());
            await ConnectWebSocketSessionAndAssert(session);
            Assert.True((await session.ExecuteOperation(operation, cts.Token).SuppressCancellationThrow()).IsCanceled);
        });

        [UnityTest]
        public IEnumerator WebSocketShouldBeClosedWhenSessionIsDisconnected() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateDefaultWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);
            var closed = false;
            WebSocketMock.When(x => x.Close(Arg.Any<WebSocketCloseCode>(), Arg.Any<string>())).Do(x => closed = true);
            DisconnectWebSocketSessionAndAssert(session);
            Assert.True(closed);
        });

        [UnityTest]
        public IEnumerator WebSocketShouldBeClosedWhenErrorOccurs() => UniTask.ToCoroutine(async () =>
        {
            const string errorMessage = "test error message";
            using var session = CreateDefaultWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);
            var closed = false;
            WebSocketMock.When(x => x.Close(Arg.Any<WebSocketCloseCode>())).Do(x => closed = true);

            WebSocketMock.OnError += Raise.Event<WebSocketErrorEventHandler>(errorMessage);

            LogAssert.Expect(LogType.Error, new Regex($".*{errorMessage}.*"));

            Assert.True(closed);

            session.Dispose();
        });

        [UnityTest]
        public IEnumerator WebSocketShouldBeClosedWhenSessionIsDisposed() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateDefaultWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);
            var closed = false;
            WebSocketMock.When(x => x.Close(Arg.Any<WebSocketCloseCode>())).Do(x => closed = true);
            session.Dispose();
            Assert.True(closed);
        });

        [UnityTest]
        public IEnumerator DisposedObjectShouldBecameUnusable() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateDefaultWebSocketSession();

            session.Dispose();

            Assert.False(session.IsConnected);
            _ = await AssertThrowsAsync<ElympicsException>(async () => await session.Connect(ConnectionDetails));
            _ = Assert.Throws<ObjectDisposedException>(() => session.Disconnect(DisconnectionReason.ApplicationShutdown));
            _ = await AssertThrowsAsync<ObjectDisposedException>(UniTask.Create(async () => await session.ExecuteOperation(new LeaveRoomDto(new Guid(1, 2, 3, Enumerable.Repeat<byte>(0, 8).ToArray())))));
        });

        [UnityTest]
        public IEnumerator ContinuationShouldRunOnTheMainThreadAfterExecuteOperationFinishesSuccessfully() => UniTask.ToCoroutine(async () =>
        {
            var operationId = new Guid("10000000000000000000000000000001");
            var operation = new JoinWithRoomIdDto(operationId, Guid.Empty, null);

            using var session = CreateDefaultWebSocketSession();
            await ConnectWebSocketSessionAndAssert(session);

            await UniTask.SwitchToMainThread();
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;

            try
            {
                _ = await session.ExecuteOperation(operation);
            }
            catch { }

            Assert.That(Thread.CurrentThread.ManagedThreadId, Is.EqualTo(mainThreadId));
        });

        [UnityTest]
        public IEnumerator ContinuationShouldRunOnTheMainThreadAfterExecuteOperationFinishesWithError() => UniTask.ToCoroutine(async () =>
        {
            var operationId = new Guid("10000000000000000000000000000001");
            var operation = new JoinWithRoomIdDto(operationId, Guid.Empty, null);

            using var session = CreateDefaultWebSocketSession();
            await ConnectWebSocketSessionAndAssert(session);

            await UniTask.SwitchToMainThread();
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;

            try
            {
                _ = await session.ExecuteOperation(operation);
            }
            catch { }

            Assert.That(Thread.CurrentThread.ManagedThreadId, Is.EqualTo(mainThreadId));
        });

        [UnityTest]
        public IEnumerator ContinuationShouldRunOnTheMainThreadAfterExecuteOperationFinishesWithTimeout() => UniTask.ToCoroutine(async () =>
        {
            var operationId = new Guid("10000000000000000000000000000001");
            var operation = new JoinWithRoomIdDto(operationId, Guid.Empty, null);

            using var session = CreateDefaultWebSocketSession();
            await ConnectWebSocketSessionAndAssert(session);
            ElympicsTimeout.WebSocketOperationTimeout = TimeSpan.Zero;

            await UniTask.SwitchToMainThread();
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;

            try
            {
                _ = await session.ExecuteOperation(operation);
            }
            catch { }

            Assert.That(Thread.CurrentThread.ManagedThreadId, Is.EqualTo(mainThreadId));
        });
        [TearDown]
        public void CleanUp()
        {
            ElympicsLogger.Log($"{nameof(TestWebSocketSession)} Cleanup");
            WebSocketMock.ClearSubstitute();
            cts.Cancel();
            ElympicsTimeout.WebSocketOpeningTimeout = DefaultOpeningTimeout;
            ElympicsTimeout.WebSocketOperationTimeout = DefaultOperationTimeout;
        }
    }
}
