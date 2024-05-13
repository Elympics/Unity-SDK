using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Lobby;
using Elympics.Lobby.Models;
using Elympics.Models.Authentication;
using Elympics.Rooms.Models;
using Elympics.Tests.Common;
using HybridWebSocket;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Elympics.Tests.Common.AsyncAsserts;
using Ping = Elympics.Lobby.Models.Ping;

#nullable enable

namespace Elympics.Tests
{
    [Category("WebSocket")]
    public class TestWebSocketSession
    {
        private static readonly AsyncEventsDispatcherMock Dispatcher = new();
        private static readonly WebSocketMock WsMock = new();
        private static readonly LobbySerializerMock SerializerMock = new();
        private static readonly AuthData AuthData = new(new Guid("10000000000000000000000000000001"), "Nickname_10000000000000000000000000000001", string.Empty);
        private static readonly SessionConnectionDetails ConnectionDetails = new("url", AuthData, default, string.Empty, string.Empty);

        private static readonly LobbySerializerMock.Methods SerializerForConnection = new()
        {
            Serialize = data => data is LobbyOperation operation ? operation.OperationId.ToByteArray() : Array.Empty<byte>(),
            Deserialize = data => data.Length == 16 ? new OperationResult(new Guid(data)) : new UnknownMessage(),
        };

        private record UnknownMessage : IFromLobby, IToLobby;
        private record UnknownOperation : LobbyOperation;

        [SetUp]
        public void ResetMocks()
        {
            WsMock.Reset();
            SerializerMock.Reset();
        }

        [UnityTest]
        public IEnumerator HappyPathConnectionScenarioShouldSucceed() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);
        });

        private static WebSocketSession CreateWebSocketSession(LobbySerializerMock.Methods? serializerMethods = null)
        {
            var session = new WebSocketSession(Dispatcher, (_, _) => WsMock, SerializerMock);
            if (serializerMethods.HasValue)
                _ = SerializerMock.UpdateMethods(serializerMethods.Value);
            return session;
        }

        private static async UniTask ConnectWebSocketSessionAndAssert(WebSocketSession session)
        {
            var oldSerializerMethods = SerializerMock.UpdateMethods(SerializerForConnection);
            WsMock.ConnectCalled += HandleConnectCalled;

            var connectedCalled = false;
            session.Connected += SetConnected;
            try
            {
                await session.Connect(ConnectionDetails);
            }
            finally
            {
                _ = SerializerMock.UpdateMethods(oldSerializerMethods);
                WsMock.ConnectCalled -= HandleConnectCalled;
                WsMock.SendCalled -= HandleMessageSent;
            }
            session.Connected -= SetConnected;

            Assert.True(connectedCalled);
            Assert.True(session.IsConnected);

            void SetConnected() => connectedCalled = true;

            void HandleConnectCalled()
            {
                WsMock.ConnectCalled -= HandleConnectCalled;
                WsMock.SendCalled += HandleMessageSent;
                WsMock.InvokeOnOpen();
            }

            void HandleMessageSent(byte[] data)
            {
                WsMock.SendCalled -= HandleMessageSent;
                WsMock.InvokeOnMessage(data);
            }
        }

        private static void DisconnectWebSocketSessionAndAssert(WebSocketSession session)
        {
            var disconnectedCalled = false;

            session.Disconnected += SetDisconnected;
            session.Disconnect();
            session.Disconnected -= SetDisconnected;

            Assert.True(disconnectedCalled);
            Assert.False(session.IsConnected);

            void SetDisconnected() => disconnectedCalled = true;
        }

        [UnityTest]
        public IEnumerator WebSocketSessionShouldAllowForReconnecting() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);
            DisconnectWebSocketSessionAndAssert(session);
            await ConnectWebSocketSessionAndAssert(session);
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenTokenIsCanceledBeforeWsOpens() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession(SerializerForConnection);

            using var cts = new CancellationTokenSource();
            WsMock.ConnectCalled += HandleConnectCalled;
            var canceled = await session.Connect(ConnectionDetails, cts.Token).SuppressCancellationThrow();

            Assert.True(canceled);
            Assert.False(session.IsConnected);

            void HandleConnectCalled()
            {
                WsMock.ConnectCalled -= HandleConnectCalled;
                cts.Cancel();
            }
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenTokenIsCanceledBeforeWsReceivesResponse() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession(SerializerForConnection);

            using var cts = new CancellationTokenSource();
            WsMock.ConnectCalled += HandleConnectCalled;
            var canceled = await session.Connect(ConnectionDetails, cts.Token).SuppressCancellationThrow();

            Assert.True(canceled);
            Assert.False(session.IsConnected);

            void HandleConnectCalled()
            {
                WsMock.ConnectCalled -= HandleConnectCalled;
                WsMock.SendCalled += HandleMessageSent;
                WsMock.InvokeOnOpen();
            }

            void HandleMessageSent(byte[] data)
            {
                WsMock.SendCalled -= HandleMessageSent;
                cts.Cancel();
            }
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenThereIsAnErrorBeforeWsOpens() => UniTask.ToCoroutine(async () =>
        {
            const string errorMessage = "test error message";
            using var session = CreateWebSocketSession(SerializerForConnection);

            WsMock.ConnectCalled += HandleConnectCalled;

            LogAssert.Expect(LogType.Error, new Regex($".*{errorMessage}.*"));
            _ = await AssertThrowsAsync<LobbyOperationException>(session.Connect(ConnectionDetails).SuppressCancellationThrow());

            Assert.False(session.IsConnected);

            static void HandleConnectCalled()
            {
                WsMock.ConnectCalled -= HandleConnectCalled;
                WsMock.InvokeOnError(errorMessage);
            }
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenThereIsAnErrorBeforeWsReceivesResponse() => UniTask.ToCoroutine(async () =>
        {
            const string errorMessage = "test error message";
            using var session = CreateWebSocketSession(SerializerForConnection);

            WsMock.ConnectCalled += HandleConnectCalled;

            LogAssert.Expect(LogType.Error, new Regex($".*{errorMessage}.*"));
            _ = await AssertThrowsAsync<LobbyOperationException>(session.Connect(ConnectionDetails).SuppressCancellationThrow());

            Assert.False(session.IsConnected);

            void HandleConnectCalled()
            {
                WsMock.ConnectCalled -= HandleConnectCalled;
                WsMock.SendCalled += HandleMessageSent;
                WsMock.InvokeOnOpen();
            }

            void HandleMessageSent(byte[] data)
            {
                WsMock.SendCalled -= HandleMessageSent;
                WsMock.InvokeOnError(errorMessage);
            }
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenThereIsAnErrorAfterConnecting() => UniTask.ToCoroutine(async () =>
        {
            const string errorMessage = "test error message";
            using var session = CreateWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);

            LogAssert.Expect(LogType.Error, new Regex($".*{errorMessage}.*"));
            WsMock.InvokeOnError(errorMessage);
            Assert.False(session.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenWsClosesBeforeItOpens() => UniTask.ToCoroutine(async () =>
        {
            const string errorMessage = "test error message";
            using var session = CreateWebSocketSession(SerializerForConnection);

            WsMock.ConnectCalled += HandleConnectCalled;

            LogAssert.Expect(LogType.Error, new Regex($".*{errorMessage}.*"));
            _ = await AssertThrowsAsync<LobbyOperationException>(session.Connect(ConnectionDetails).SuppressCancellationThrow());

            Assert.False(session.IsConnected);

            static void HandleConnectCalled()
            {
                WsMock.ConnectCalled -= HandleConnectCalled;
                WsMock.InvokeOnClose(WebSocketCloseCode.Abnormal, errorMessage);
            }
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenWsClosesBeforeItReceivesResponse() => UniTask.ToCoroutine(async () =>
        {
            const string errorMessage = "test error message";
            using var session = CreateWebSocketSession(SerializerForConnection);

            WsMock.ConnectCalled += HandleConnectCalled;

            LogAssert.Expect(LogType.Error, new Regex($".*{errorMessage}.*"));
            _ = await AssertThrowsAsync<LobbyOperationException>(session.Connect(ConnectionDetails).SuppressCancellationThrow());

            Assert.False(session.IsConnected);

            void HandleConnectCalled()
            {
                WsMock.ConnectCalled -= HandleConnectCalled;
                WsMock.SendCalled += HandleMessageSent;
                WsMock.InvokeOnOpen();
            }

            void HandleMessageSent(byte[] data)
            {
                WsMock.SendCalled -= HandleMessageSent;
                WsMock.InvokeOnClose(WebSocketCloseCode.Abnormal, errorMessage);
            }
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenOpeningWsTakesTooLong() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession(SerializerForConnection);
            session.OpeningTimeout = TimeSpan.Zero;

            WsMock.ConnectCalled += HandleConnectCalled;

            _ = await AssertThrowsAsync<LobbyOperationException>(session.Connect(ConnectionDetails));
            Assert.False(session.IsConnected);

            void HandleConnectCalled()
            {
                WsMock.ConnectCalled -= HandleConnectCalled;
                WsMock.SendCalled += HandleMessageSent;
                UniTask.Delay(TimeSpan.FromSeconds(0.1)).ContinueWith(() => WsMock.InvokeOnOpen()).Forget();
            }

            void HandleMessageSent(byte[] data)
            {
                WsMock.SendCalled -= HandleMessageSent;
                Assert.Fail("WebSocket has been opened.");
            }
        });

        [UnityTest]
        public IEnumerator ConnectionShouldBeAbortedWhenReceivingResponseTakesTooLong() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession(SerializerForConnection);
            session.OperationTimeout = TimeSpan.Zero;

            WsMock.ConnectCalled += HandleConnectCalled;

            _ = await AssertThrowsAsync<LobbyOperationException>(session.Connect(ConnectionDetails));
            Assert.False(session.IsConnected);

            void HandleConnectCalled()
            {
                WsMock.ConnectCalled -= HandleConnectCalled;
                WsMock.SendCalled += HandleMessageSent;
                WsMock.InvokeOnOpen();
            }

            void HandleMessageSent(byte[] data)
            {
                WsMock.SendCalled -= HandleMessageSent;
                UniTask.Delay(TimeSpan.FromSeconds(5)).ContinueWith(() => WsMock.InvokeOnMessage(data)).Forget();
            }
        });

        [UnityTest]
        public IEnumerator PingMessageShouldNotBePassed() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession(new LobbySerializerMock.Methods
            {
                Serialize = _ => Array.Empty<byte>(),
                Deserialize = _ => new Ping(),
            });

            await ConnectWebSocketSessionAndAssert(session);

            IFromLobby? passedMessage = null;
            session.MessageReceived += ReceiveMessage;
            WsMock.InvokeOnMessage(Array.Empty<byte>());
            Assert.IsNull(passedMessage);

            void ReceiveMessage(IFromLobby message) => passedMessage = message;
        });

        [UnityTest]
        public IEnumerator OperationResultMessageShouldNotBePassed() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession(new LobbySerializerMock.Methods
            {
                Serialize = _ => Array.Empty<byte>(),
                Deserialize = _ => new OperationResult(Guid.Empty),
            });

            await ConnectWebSocketSessionAndAssert(session);

            IFromLobby? passedMessage = null;
            session.MessageReceived += ReceiveMessage;
            WsMock.InvokeOnMessage(Array.Empty<byte>());
            Assert.IsNull(passedMessage);

            void ReceiveMessage(IFromLobby message) => passedMessage = message;
        });

        [UnityTest]
        public IEnumerator OtherMessageTypesShouldBePassed() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession(new LobbySerializerMock.Methods
            {
                Serialize = _ => Array.Empty<byte>(),
                Deserialize = _ => new UnknownMessage(),
            });

            await ConnectWebSocketSessionAndAssert(session);

            IFromLobby? passedMessage = null;
            session.MessageReceived += ReceiveMessage;
            WsMock.InvokeOnMessage(Array.Empty<byte>());
            Assert.IsInstanceOf<UnknownMessage>(passedMessage);

            void ReceiveMessage(IFromLobby message) => passedMessage = message;
        });

        [UnityTest]
        public IEnumerator MessageShouldBeReceivedOnceAfterReconnecting() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession(new LobbySerializerMock.Methods
            {
                Serialize = _ => Array.Empty<byte>(),
                Deserialize = _ => new UnknownMessage(),
            });
            var counter = 0;
            await ConnectWebSocketSessionAndAssert(session);
            DisconnectWebSocketSessionAndAssert(session);
            await ConnectWebSocketSessionAndAssert(session);


            session.MessageReceived += ReceiveMessage;
            WsMock.InvokeOnMessage(Array.Empty<byte>());

            void ReceiveMessage(IFromLobby message) => counter++;
            Assert.AreEqual(1, counter);
        });

        [UnityTest]
        public IEnumerator HappyPathOperationExecutionScenarioShouldSucceed() => UniTask.ToCoroutine(async () =>
        {
            var operation = new UnknownOperation();
            using var session = CreateWebSocketSession(new LobbySerializerMock.Methods
            {
                Serialize = _ => Array.Empty<byte>(),
                Deserialize = _ => new OperationResult(operation.OperationId),
            });

            await ConnectWebSocketSessionAndAssert(session);

            WsMock.SendCalled += HandleMessageSent;
            _ = await session.ExecuteOperation(operation);

            static void HandleMessageSent(byte[] data)
            {
                WsMock.SendCalled -= HandleMessageSent;
                WsMock.InvokeOnMessage(Array.Empty<byte>());
            }
        });

        [UnityTest]
        public IEnumerator OperationShouldNotBePossibleToExecuteBeforeConnecting() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession(new LobbySerializerMock.Methods
            {
                Serialize = _ => Array.Empty<byte>(),
            });
            var operation = new UnknownOperation();
            WsMock.SendCalled += HandleMessageSent;

            LogAssert.Expect(LogType.Exception, new Regex($".*{nameof(InvalidOperationException)}.*"));
            _ = await AssertThrowsAsync<InvalidOperationException>(UniTask.Create(async () => await session.ExecuteOperation(operation)));

            static void HandleMessageSent(byte[] data)
            {
                WsMock.SendCalled -= HandleMessageSent;
                Assert.Fail("Operation has been sent.");
            }
        });

        [UnityTest]
        public IEnumerator OperationShouldBeAbortedWhenReceivingResponseTakesTooLong() => UniTask.ToCoroutine(async () =>
        {
            var operation = new UnknownOperation();
            using var session = CreateWebSocketSession(new LobbySerializerMock.Methods
            {
                Serialize = _ => Array.Empty<byte>(),
                Deserialize = _ => new OperationResult(operation.OperationId),
            });
            session.OperationTimeout = TimeSpan.Zero;

            await ConnectWebSocketSessionAndAssert(session);
            WsMock.SendCalled += HandleMessageSent;

            _ = await AssertThrowsAsync<LobbyOperationException>(session.ExecuteOperation(operation));

            void HandleMessageSent(byte[] data)
            {
                WsMock.SendCalled -= HandleMessageSent;
                UniTask.Delay(TimeSpan.FromSeconds(0.1)).ContinueWith(() => WsMock.InvokeOnMessage(data)).Forget();
            }
        });

        [UnityTest]
        public IEnumerator OperationShouldBeCancellable() => UniTask.ToCoroutine(async () =>
        {
            var operation = new UnknownOperation();
            using var session = CreateWebSocketSession(new LobbySerializerMock.Methods
            {
                Serialize = _ => Array.Empty<byte>(),
                Deserialize = _ => new OperationResult(operation.OperationId),
            });
            using var cts = new CancellationTokenSource();

            await ConnectWebSocketSessionAndAssert(session);
            WsMock.SendCalled += HandleMessageSent;

            Assert.True((await session.ExecuteOperation(operation, cts.Token).SuppressCancellationThrow()).IsCanceled);

            void HandleMessageSent(byte[] data)
            {
                WsMock.SendCalled -= HandleMessageSent;
                cts.Cancel();
            }
        });

        [UnityTest]
        public IEnumerator WebSocketShouldBeClosedWhenSessionIsDisconnected() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);
            var closed = false;
            WsMock.CloseCalled += SetClosed;

            DisconnectWebSocketSessionAndAssert(session);
            WsMock.CloseCalled -= SetClosed;

            Assert.True(closed);

            void SetClosed(WebSocketCloseCode closeCode, string? reason) => closed = true;
        });

        [UnityTest]
        public IEnumerator WebSocketShouldBeClosedWhenErrorOccurs() => UniTask.ToCoroutine(async () =>
        {
            const string errorMessage = "test error message";
            using var session = CreateWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);
            var closed = false;
            WsMock.CloseCalled += SetClosed;

            WsMock.InvokeOnError(errorMessage);
            LogAssert.Expect(LogType.Error, new Regex($".*{errorMessage}.*"));
            WsMock.CloseCalled -= SetClosed;

            Assert.True(closed);

            session.Dispose();

            void SetClosed(WebSocketCloseCode closeCode, string? reason) => closed = true;
        });

        [UnityTest]
        public IEnumerator WebSocketShouldBeClosedWhenSessionIsDisposed() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession();

            await ConnectWebSocketSessionAndAssert(session);
            var closed = false;
            WsMock.CloseCalled += SetClosed;

            session.Dispose();
            WsMock.CloseCalled -= SetClosed;

            Assert.True(closed);

            void SetClosed(WebSocketCloseCode closeCode, string? reason) => closed = true;
        });

        [UnityTest]
        public IEnumerator DisposedObjectShouldBecameUnusable() => UniTask.ToCoroutine(async () =>
        {
            using var session = CreateWebSocketSession(SerializerForConnection);

            session.Dispose();

            Assert.False(session.IsConnected);
            _ = await AssertThrowsAsync<ObjectDisposedException>(session.Connect(ConnectionDetails));
            _ = Assert.Throws<ObjectDisposedException>(session.Disconnect);
            _ = await AssertThrowsAsync<ObjectDisposedException>(UniTask.Create(async () => await session.ExecuteOperation(new LeaveRoom(new Guid(1, 2, 3, Enumerable.Repeat<byte>(0, 8).ToArray())))));
        });

        [UnityTest]
        public IEnumerator ContinuationShouldRunOnTheMainThreadAfterExecuteOperationFinishesSuccessfully() => UniTask.ToCoroutine(async () =>
        {
            var operationId = new Guid("10000000000000000000000000000001");
            var operation = new JoinWithRoomId(operationId, Guid.Empty, null);

            using var session = CreateWebSocketSession(new LobbySerializerMock.Methods
            {
                Serialize = _ => Array.Empty<byte>(),
                Deserialize = _ => new OperationResult(operationId),
            });
            await ConnectWebSocketSessionAndAssert(session);

            await UniTask.SwitchToMainThread();
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;

            WsMock.SendCalled += _ => UniTask.Create(async () =>
            {
                await UniTask.SwitchToThreadPool();
                WsMock.InvokeOnMessage(Array.Empty<byte>());
            });

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
            var operation = new JoinWithRoomId(operationId, Guid.Empty, null);

            using var session = CreateWebSocketSession(new LobbySerializerMock.Methods
            {
                Serialize = _ => Array.Empty<byte>(),
                Deserialize = _ => new OperationResult(operationId, ErrorBlame.UserError, ErrorKind.Unspecified, null),
            });
            await ConnectWebSocketSessionAndAssert(session);

            await UniTask.SwitchToMainThread();
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;

            WsMock.SendCalled += _ => UniTask.Create(async () =>
            {
                await UniTask.SwitchToThreadPool();
                WsMock.InvokeOnMessage(Array.Empty<byte>());
            });

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
            var operation = new JoinWithRoomId(operationId, Guid.Empty, null);

            using var session = CreateWebSocketSession(SerializerForConnection);
            await ConnectWebSocketSessionAndAssert(session);
            session.OperationTimeout = TimeSpan.Zero;

            await UniTask.SwitchToMainThread();
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;

            try
            {
                _ = await session.ExecuteOperation(operation);
            }
            catch { }

            Assert.That(Thread.CurrentThread.ManagedThreadId, Is.EqualTo(mainThreadId));
        });
    }
}
