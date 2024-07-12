#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Elympics.Lobby;
using HybridWebSocket;
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

        public static ElympicsLobbyClient MockIWebSocket(
            this ElympicsLobbyClient sut,
            Guid userId,
            string nickname,
            bool createInitialRoom,
            double? pingDelay,
            out IWebSocket createdMock)
        {
            var mock = WebSocketMockSetup.CreateMockWebSocket(userId, nickname, createInitialRoom, pingDelay);
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

        public static ElympicsLobbyClient MockSuccessIAuthClient(this ElympicsLobbyClient sut, string jwt, Guid userId, string nickname)
        {
            var mockAuthClient = AuthClientMockSetup.CreateSuccessIAuthClient(jwt, userId, nickname);
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
