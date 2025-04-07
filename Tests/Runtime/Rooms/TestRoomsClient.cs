using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Lobby.Models;
using Elympics.Models.Authentication;
using Elympics.Rooms.Models;
using Elympics.Tests.Common.RoomMocks;
using NUnit.Framework;
using UnityEngine.TestTools;

#nullable enable

namespace Elympics.Tests.Rooms
{
    [Category("Rooms")]
    internal class TestRoomsClient
    {
        private static readonly WebSocketSessionMock WsSessionMock = new();

        private static readonly RoomsClient RoomsClient = new(new ElympicsLoggerContext(Guid.Empty))
        {
            Session = WsSessionMock,
        };

        private static readonly Guid TestRoomGuid = new(1234, 56, 78, Enumerable.Repeat<byte>(90, 8).ToArray());
        private static readonly Guid TestHostGuid = new(999, 666, 55, Enumerable.Repeat<byte>(20, 8).ToArray());

        private static readonly string TestHostNickname = "Nickname_" + TestHostGuid;

        [SetUp]
        public void ResetMocks()
        {
            WsSessionMock.Reset();
            WsSessionMock.ConnectionDetails = new SessionConnectionDetails("url", new AuthData(TestHostGuid, TestHostNickname, null), Guid.Empty, "", "");
            RoomsClient.Session = WsSessionMock;
        }

        [UnityTest]
        public IEnumerator CreatingRoomShouldExecuteCorrectly() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new CreateRoom("test room name", true, false, "test queue", true, new Dictionary<string, string>(), new Dictionary<string, string>());
            WsSessionMock.ResultToReturn = new RoomIdOperationResult(Guid.Empty, TestRoomGuid);

            // Act
            _ = await RoomsClient.CreateRoom(expectedMessage.RoomName,
                expectedMessage.IsPrivate,
                expectedMessage.IsEphemeral,
                expectedMessage.QueueName,
                expectedMessage.IsSingleTeam,
                expectedMessage.CustomRoomData,
                expectedMessage.CustomMatchmakingData);

            Assert.That(WsSessionMock.ExecutedOperations, Has.Count.EqualTo(1));
            Assert.That(WsSessionMock.ExecutedOperations[0], Is.EqualTo(expectedMessage));
        });

        [UnityTest]
        public IEnumerator CreatingRoomShouldThrowIfThereIsNoSession() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new CreateRoom("test room name", true, false, "test queue", true, new Dictionary<string, string>(), new Dictionary<string, string>());
            RoomsClient.Session = null;

            // Act
            var result = await ((UniTask)UniTask.Create(async () => await RoomsClient.CreateRoom(expectedMessage.RoomName,
                expectedMessage.IsPrivate,
                expectedMessage.IsEphemeral,
                expectedMessage.QueueName,
                expectedMessage.IsSingleTeam,
                expectedMessage.CustomRoomData,
                expectedMessage.CustomMatchmakingData))).Catch();

            Assert.That(result, Is.InstanceOf<InvalidOperationException>());
            Assert.That(WsSessionMock.ExecutedOperations, Is.Empty);
        });

        [UnityTest]
        public IEnumerator CreatingRoomShouldThrowIfIncompatibleResultIsReturned() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new CreateRoom("test room name", true, false, "test queue", true, new Dictionary<string, string>(), new Dictionary<string, string>());
            WsSessionMock.ResultToReturn = new OperationResult(Guid.Empty);

            // Act
            var result = await UniTask.Create(async () => await (UniTask)RoomsClient.CreateRoom(expectedMessage.RoomName,
                expectedMessage.IsPrivate,
                expectedMessage.IsEphemeral,
                expectedMessage.QueueName,
                expectedMessage.IsSingleTeam,
                expectedMessage.CustomRoomData,
                expectedMessage.CustomMatchmakingData)).Catch();

            Assert.That(result, Is.InstanceOf<UnexpectedRoomResultException>());
            Assert.That(WsSessionMock.ExecutedOperations, Has.Count.EqualTo(1));
            Assert.That(WsSessionMock.ExecutedOperations[0], Is.EqualTo(expectedMessage));
        });

        [UnityTest]
        public IEnumerator JoiningRoomByIdShouldExecuteCorrectly() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new JoinWithRoomId(TestRoomGuid, null);
            WsSessionMock.ResultToReturn = new RoomIdOperationResult(Guid.Empty, TestRoomGuid);

            // Act
            _ = await RoomsClient.JoinRoom(expectedMessage.RoomId, expectedMessage.TeamIndex);

            Assert.That(WsSessionMock.ExecutedOperations, Has.Count.EqualTo(1));
            Assert.That(WsSessionMock.ExecutedOperations[0], Is.EqualTo(expectedMessage));
        });

        [UnityTest]
        public IEnumerator JoiningRoomByIdShouldThrowIfThereIsNoSession() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new JoinWithRoomId(TestRoomGuid, null);
            RoomsClient.Session = null;

            // Act
            var result = await ((UniTask)UniTask.Create(async () => await RoomsClient.JoinRoom(expectedMessage.RoomId, expectedMessage.TeamIndex))).Catch();

            Assert.That(result, Is.InstanceOf<InvalidOperationException>());
            Assert.That(WsSessionMock.ExecutedOperations, Is.Empty);
        });

        [UnityTest]
        public IEnumerator JoiningRoomByIdShouldThrowIfIncompatibleResultIsReturned() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new JoinWithRoomId(TestRoomGuid, null);
            WsSessionMock.ResultToReturn = new OperationResult(Guid.Empty);

            // Act
            var result = await ((UniTask)UniTask.Create(async () => await RoomsClient.JoinRoom(expectedMessage.RoomId, expectedMessage.TeamIndex))).Catch();

            Assert.That(result, Is.InstanceOf<UnexpectedRoomResultException>());
            Assert.That(WsSessionMock.ExecutedOperations, Has.Count.EqualTo(1));
            Assert.That(WsSessionMock.ExecutedOperations[0], Is.EqualTo(expectedMessage));
        });

        [UnityTest]
        public IEnumerator JoiningRoomUsingJoinCodeShouldExecuteCorrectly() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new JoinWithJoinCode("test join code", null);
            WsSessionMock.ResultToReturn = new RoomIdOperationResult(Guid.Empty, TestRoomGuid);

            // Act
            _ = await RoomsClient.JoinRoom(expectedMessage.JoinCode, expectedMessage.TeamIndex);

            Assert.That(WsSessionMock.ExecutedOperations, Has.Count.EqualTo(1));
            Assert.That(WsSessionMock.ExecutedOperations[0], Is.EqualTo(expectedMessage));
        });

        [UnityTest]
        public IEnumerator JoiningRoomUsingJoinCodeShouldThrowIfThereIsNoSession() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new JoinWithJoinCode("test join code", null);
            RoomsClient.Session = null;

            // Act
            var result = await ((UniTask)UniTask.Create(async () => await RoomsClient.JoinRoom(expectedMessage.JoinCode, expectedMessage.TeamIndex))).Catch();

            Assert.That(result, Is.InstanceOf<InvalidOperationException>());
            Assert.That(WsSessionMock.ExecutedOperations, Is.Empty);
        });

        [UnityTest]
        public IEnumerator JoiningRoomUsingJoinCodeShouldThrowIfIncompatibleResultIsReturned() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new JoinWithJoinCode("test join code", null);
            WsSessionMock.ResultToReturn = new OperationResult(Guid.Empty);

            // Act
            var result = await ((UniTask)UniTask.Create(async () => await RoomsClient.JoinRoom(expectedMessage.JoinCode, expectedMessage.TeamIndex))).Catch();

            Assert.That(result, Is.InstanceOf<UnexpectedRoomResultException>());
            Assert.That(WsSessionMock.ExecutedOperations, Has.Count.EqualTo(1));
            Assert.That(WsSessionMock.ExecutedOperations[0], Is.EqualTo(expectedMessage));
        });

        [UnityTest]
        public IEnumerator ChangingTeamShouldExecuteCorrectly() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new ChangeTeam(TestRoomGuid, 5);

            // Act
            await RoomsClient.ChangeTeam(expectedMessage.RoomId, expectedMessage.TeamIndex);

            Assert.That(WsSessionMock.ExecutedOperations, Has.Count.EqualTo(1));
            Assert.That(WsSessionMock.ExecutedOperations[0], Is.EqualTo(expectedMessage));
        });

        [UnityTest]
        public IEnumerator ChangingTeamShouldThrowIfThereIsNoSession() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new ChangeTeam(TestRoomGuid, 5);
            RoomsClient.Session = null;

            // Act
            var result = await UniTask.Create(async () => await RoomsClient.ChangeTeam(expectedMessage.RoomId, expectedMessage.TeamIndex)).Catch();

            Assert.That(result, Is.InstanceOf<InvalidOperationException>());
            Assert.That(WsSessionMock.ExecutedOperations, Is.Empty);
        });

        [UnityTest]
        public IEnumerator SettingReadyShouldExecuteCorrectly() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new SetReady(TestRoomGuid,
                new byte[]
                {
                    1,
                    2,
                    3,
                },
                new[]
                {
                    0.1f,
                    0.2f,
                    0.3f,
                });

            // Act
            await RoomsClient.SetReady(expectedMessage.RoomId, expectedMessage.GameEngineData, expectedMessage.MatchmakerData);

            Assert.That(WsSessionMock.ExecutedOperations, Has.Count.EqualTo(1));
            Assert.That(WsSessionMock.ExecutedOperations[0], Is.EqualTo(expectedMessage));
        });

        [UnityTest]
        public IEnumerator SettingReadyShouldThrowIfThereIsNoSession() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new SetReady(TestRoomGuid,
                new byte[]
                {
                    1,
                    2,
                    3,
                },
                new[]
                {
                    0.1f,
                    0.2f,
                    0.3f,
                });
            RoomsClient.Session = null;

            // Act
            var result = await UniTask.Create(async () => await RoomsClient.SetReady(expectedMessage.RoomId, expectedMessage.GameEngineData, expectedMessage.MatchmakerData)).Catch();

            Assert.That(result, Is.InstanceOf<InvalidOperationException>());
            Assert.That(WsSessionMock.ExecutedOperations, Is.Empty);
        });

        [UnityTest]
        public IEnumerator SettingUnreadyShouldExecuteCorrectly() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new SetUnready(TestRoomGuid);

            // Act
            await RoomsClient.SetUnready(expectedMessage.RoomId);

            Assert.That(WsSessionMock.ExecutedOperations, Has.Count.EqualTo(1));
            Assert.That(WsSessionMock.ExecutedOperations[0], Is.EqualTo(expectedMessage));
        });

        [UnityTest]
        public IEnumerator SettingUnreadyShouldThrowIfThereIsNoSession() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new SetUnready(TestRoomGuid);
            RoomsClient.Session = null;

            // Act
            var result = await UniTask.Create(async () => await RoomsClient.SetUnready(expectedMessage.RoomId)).Catch();

            Assert.That(result, Is.InstanceOf<InvalidOperationException>());
            Assert.That(WsSessionMock.ExecutedOperations, Is.Empty);
        });

        [UnityTest]
        public IEnumerator LeavingRoomShouldExecuteCorrectly() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new LeaveRoom(TestRoomGuid);

            // Act
            await RoomsClient.LeaveRoom(expectedMessage.RoomId);

            Assert.That(WsSessionMock.ExecutedOperations, Has.Count.EqualTo(1));
            Assert.That(WsSessionMock.ExecutedOperations[0], Is.EqualTo(expectedMessage));
        });

        [UnityTest]
        public IEnumerator LeavingRoomShouldThrowIfThereIsNoSession() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new LeaveRoom(TestRoomGuid);
            RoomsClient.Session = null;

            // Act
            var result = await UniTask.Create(async () => await RoomsClient.LeaveRoom(expectedMessage.RoomId)).Catch();

            Assert.That(result, Is.InstanceOf<InvalidOperationException>());
            Assert.That(WsSessionMock.ExecutedOperations, Is.Empty);
        });

        [UnityTest]
        public IEnumerator StartingMatchmakingShouldExecuteCorrectly() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new StartMatchmaking(TestRoomGuid);

            // Act
            await RoomsClient.StartMatchmaking(expectedMessage.RoomId, TestHostGuid);

            Assert.That(WsSessionMock.ExecutedOperations, Has.Count.EqualTo(1));
            Assert.That(WsSessionMock.ExecutedOperations[0], Is.EqualTo(expectedMessage));
        });

        [UnityTest]
        public IEnumerator StartingMatchmakingShouldThrowIfThereIsNoSession() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new StartMatchmaking(TestRoomGuid);
            RoomsClient.Session = null;

            // Act
            var result = await UniTask.Create(async () => await RoomsClient.StartMatchmaking(expectedMessage.RoomId, TestHostGuid)).Catch();

            Assert.That(result, Is.InstanceOf<InvalidOperationException>());
            Assert.That(WsSessionMock.ExecutedOperations, Is.Empty);
        });

        [UnityTest]
        public IEnumerator CancelingMatchmakingShouldExecuteCorrectly() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new CancelMatchmaking(TestRoomGuid);

            // Act
            await RoomsClient.CancelMatchmaking(expectedMessage.RoomId);

            Assert.That(WsSessionMock.ExecutedOperations, Has.Count.EqualTo(1));
            Assert.That(WsSessionMock.ExecutedOperations[0], Is.EqualTo(expectedMessage));
        });

        [UnityTest]
        public IEnumerator CancelingMatchmakingShouldThrowIfThereIsNoSession() => UniTask.ToCoroutine(async () =>
        {
            var expectedMessage = new CancelMatchmaking(TestRoomGuid);
            RoomsClient.Session = null;

            // Act
            var result = await UniTask.Create(async () => await RoomsClient.CancelMatchmaking(expectedMessage.RoomId)).Catch();

            Assert.That(result, Is.InstanceOf<InvalidOperationException>());
            Assert.That(WsSessionMock.ExecutedOperations, Is.Empty);
        });

        public record ProperlyHandledMessageTestCase(IFromLobby Message, string ExpectedEventName, object ExpectedEventArgs);

        private static readonly List<ProperlyHandledMessageTestCase> ProperlyHandledMessageTestCases = new()
        {
            new ProperlyHandledMessageTestCase(
                new RoomStateChanged(TestRoomGuid, DateTime.UnixEpoch, "test room name", null, true, null, new List<UserInfo>(), false, false, new Dictionary<string, string>()),
                nameof(IRoomsClient.RoomStateChanged),
                new RoomStateChanged(TestRoomGuid, DateTime.UnixEpoch, "test room name", null, true, null, new List<UserInfo>(), false, false, new Dictionary<string, string>())),
            new ProperlyHandledMessageTestCase(new RoomWasLeft(TestRoomGuid, LeavingReason.UserLeft), nameof(IRoomsClient.LeftRoom), new LeftRoomArgs(TestRoomGuid, LeavingReason.UserLeft)),
        };

        [Test]
        public void HandledMessagesShouldBePassedProperly([ValueSource(nameof(ProperlyHandledMessageTestCases))] ProperlyHandledMessageTestCase testCase)
        {
            using var eventScope = new RoomsClientEventScope(RoomsClient);

            // Act
            WsSessionMock.InvokeMessageReceived(testCase.Message);

            Assert.That(eventScope.InvokedEventName, Is.EqualTo(testCase.ExpectedEventName));
            Assert.That(eventScope.InvokedEventArgs, Is.EqualTo(testCase.ExpectedEventArgs));
        }

        private record UnknownMessage : IFromLobby;

        private static readonly List<IFromLobby> UnsupportedMessages = new()
        {
            new Ping(),
            new Pong(),
            new UnknownMessage(),
            new OperationResult(Guid.Empty),
        };

        [Test]
        public void UnsupportedMessagesShouldNotBePassed([ValueSource(nameof(UnsupportedMessages))] IFromLobby message)
        {
            using var eventScope = new RoomsClientEventScope(RoomsClient);

            // Act
            WsSessionMock.InvokeMessageReceived(message);

            Assert.That(eventScope.InvokedEventName, Is.Null);
            Assert.That(eventScope.InvokedEventArgs, Is.Null);
        }

        private class RoomsClientEventScope : IDisposable
        {
            private IRoomsClient? _roomsClient;

            public string? InvokedEventName { get; private set; }
            public object? InvokedEventArgs { get; private set; }

            public RoomsClientEventScope(IRoomsClient roomsClient)
            {
                _roomsClient = roomsClient;
                _roomsClient.RoomListChanged += OnRoomListChanged;
                _roomsClient.RoomStateChanged += OnRoomStateChanged;
                _roomsClient.LeftRoom += OnLeftRoom;
            }

            private void OnRoomListChanged(RoomListChanged args)
            {
                InvokedEventName = nameof(_roomsClient.RoomListChanged);
                InvokedEventArgs = args;
            }

            private void OnRoomStateChanged(RoomStateChanged args)
            {
                InvokedEventName = nameof(_roomsClient.RoomStateChanged);
                InvokedEventArgs = args;
            }

            private void OnLeftRoom(LeftRoomArgs args)
            {
                InvokedEventName = nameof(_roomsClient.LeftRoom);
                InvokedEventArgs = args;
            }

            public void Dispose()
            {
                if (_roomsClient == null)
                    return;
                _roomsClient.RoomListChanged -= OnRoomListChanged;
                _roomsClient.RoomStateChanged -= OnRoomStateChanged;
                _roomsClient.LeftRoom -= OnLeftRoom;
                _roomsClient = null;
            }
        }
    }
}
