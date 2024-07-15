#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Elympics.Lobby;
using Elympics.Models.Authentication;
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

        public static ElympicsLobbyClient MockIWebSocket(this ElympicsLobbyClient sut, Guid userId, string nickname, bool createInitialRoom, double? pingDelay)
        {

            var webSocketSessionField = sut.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == WebSocketSessionName);
            Assert.NotNull(webSocketSessionField);

            var lazyWebSocketObject = (Lazy<WebSocketSession>)webSocketSessionField.GetValue(sut);
            var webSocketFactory = lazyWebSocketObject.Value.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == WebSocketFactory);
            Assert.NotNull(webSocketFactory);
            webSocketFactory.SetValue(lazyWebSocketObject.Value, (WebSocketSession.WebSocketFactory)MockWebSocket);
            var dispatcher = lazyWebSocketObject.Value.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == AsyncEventDispatcherFieldName);
            Assert.NotNull(dispatcher);
            dispatcher.SetValue(lazyWebSocketObject.Value, AsyncEventsDispatcherMockSetup.CreateMockAsyncEventsDispatcher());
            return sut;

#pragma warning disable IDE0062
            IWebSocket MockWebSocket(string s, string? s1) => WebSocketMockSetup.CreateMockWebSocket(userId, nickname, createInitialRoom, pingDelay);
#pragma warning restore IDE0062
        }
        public static ElympicsLobbyClient MockIAuthClient(this ElympicsLobbyClient sut, string jwt, Guid userId, string nickname, AuthType authType)
        {
            var mockAuthClient = AuthClientMockSetup.CreateDefaultIAuthClient(jwt, userId, nickname, authType);
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
