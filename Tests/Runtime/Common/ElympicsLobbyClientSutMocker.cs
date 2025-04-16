#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Elympics.Lobby;
using Elympics.Rooms.Models;
using HybridWebSocket;
using NSubstitute;
using NUnit.Framework;

namespace Elympics.Tests
{
    internal static class ElympicsLobbyClientSutMocker
    {
        private const string WebSocketSessionName = "_webSocketSession";
        private const string PingTimeoutThresholdFieldName = "_automaticDisconnectThreshold";
        private const string AsyncEventDispatcherFieldName = "_dispatcher";
        private const string WebSocketFactory = "_wsFactory";
        private const string AuthClientFieldName = "_auth";
        private const string AvailableRegionRetriever = "_regionRetriever";
        private const string RoomsManager = "_roomsManager";

        public static ElympicsLobbyClient MockIWebSocket(
            this ElympicsLobbyClient sut,
            Guid userId,
            string nickname,
            string? avatarUrl,
            bool createInitialRoom,
            double? pingDelay,
            out IWebSocket createdMock)
        {
            var mock = WebSocketMockSetup.CreateMockWebSocket(userId, nickname, avatarUrl, createInitialRoom, pingDelay);
            SetIWebSocketMock(sut, mock);
            createdMock = mock;
            return sut;
        }

        private static void SetIWebSocketMock(ElympicsLobbyClient sut, IWebSocket mock)
        {
            var webSocketSessionField = sut.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == WebSocketSessionName);
            Assert.NotNull(webSocketSessionField);

            var lazyWebSocketObject = (Lazy<WebSocketSession>)webSocketSessionField.GetValue(sut);
            var webSocketFactory = lazyWebSocketObject.Value.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == WebSocketFactory);
            Assert.NotNull(webSocketFactory);

            webSocketFactory.SetValue(lazyWebSocketObject.Value, (WebSocketSession.WebSocketFactory)((string url, string? protocol) => mock));
            var dispatcher = lazyWebSocketObject.Value.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == AsyncEventDispatcherFieldName);
            Assert.NotNull(dispatcher);
            dispatcher.SetValue(lazyWebSocketObject.Value, AsyncEventsDispatcherMockSetup.CreateMockAsyncEventsDispatcher());
#pragma warning disable IDE0062
            //IWebSocket MockWebSocket(string s, string? s1) => () => mock;
#pragma warning restore IDE0062
        }

        public static ElympicsLobbyClient MockSuccessIAuthClient(this ElympicsLobbyClient sut, Guid userId, string nickname)
        {
            var mockAuthClient = AuthClientMockSetup.CreateSuccessIAuthClient(userId, nickname);
            var authField = sut!.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == AuthClientFieldName);
            Assert.NotNull(authField);
            authField.SetValue(sut, mockAuthClient);
            return sut;
        }

        public static ElympicsLobbyClient MockFailureIAuthClient(this ElympicsLobbyClient sut)
        {
            var mockAuthClient = AuthClientMockSetup.CreateFailureIAuthClient();
            var authField = sut!.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == AuthClientFieldName);
            Assert.NotNull(authField);
            authField.SetValue(sut, mockAuthClient);
            return sut;
        }

        public static ElympicsLobbyClient MockIAvailableRegionRetriever(this ElympicsLobbyClient sut, params string[] regions)
        {
            var availableRegionRetriever = sut.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == AvailableRegionRetriever);
            Assert.NotNull(availableRegionRetriever);

            var regionRetrieverMock = Substitute.For<IAvailableRegionRetriever>();
            _ = regionRetrieverMock.GetAvailableRegions().Returns(UniTask.FromResult(new List<string>(regions)));
            availableRegionRetriever.SetValue(sut, regionRetrieverMock);
            return sut;
        }

        public static ElympicsLobbyClient MockIRoomManager(this ElympicsLobbyClient sut)
        {
            var lazyRoomsManager = sut.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == RoomsManager);
            Assert.NotNull(lazyRoomsManager);
            var roomManagerMock = Substitute.For<IRoomsManager>();
            var roomClient = Substitute.For<IRoomsClient>();
            _ = roomClient.StartMatchmaking(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(UniTask.CompletedTask);
#pragma warning disable IDE0017
            IRoom room = new Room(sut,
                roomClient,
                Guid.Empty,
                new RoomStateChanged(Guid.Empty,
                    DateTime.Now,
                    string.Empty,
                    null,
                    false,
                    new MatchmakingData(DateTime.Now,
                        MatchmakingState.Playing,
                        "test",
                        1,
                        1,
                        new Dictionary<string, string>(),
                        new MatchData(Guid.Empty, MatchState.Running, new MatchDetails(new List<Guid>(), null, null, null, null, null), null),
                        null,
                        null),
                    new List<UserInfo>() { new(Guid.Empty, 0, true, string.Empty, string.Empty) },
                    false,
                    false,
                    null));
            room.ToggleJoinStatus(true);
#pragma warning restore IDE0017
            _ = roomManagerMock.ListJoinedRooms().Returns(new List<IRoom>()
            {
                room
            });

            var lazy = new Lazy<IRoomsManager>(roomManagerMock);
            lazyRoomsManager.SetValue(sut, lazy);
            return sut;
        }

        public static ElympicsLobbyClient SetPingThresholdTimeout(this ElympicsLobbyClient sut, TimeSpan newTimeout)
        {
            var webSocketSessionField = sut.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == WebSocketSessionName);
            Assert.NotNull(webSocketSessionField);

            var lazyWebSocketObject = (Lazy<WebSocketSession>)webSocketSessionField.GetValue(sut);
            var webSocketSession = lazyWebSocketObject.Value;
            var pingDisconnectTimeout = webSocketSession.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == PingTimeoutThresholdFieldName);

            pingDisconnectTimeout!.SetValue(webSocketSession, newTimeout);
            return sut;
        }
    }
}
