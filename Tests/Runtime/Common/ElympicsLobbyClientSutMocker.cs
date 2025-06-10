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

        public static ElympicsLobbyClient InjectMockIWebSocket(this ElympicsLobbyClient sut, IWebSocket mock)
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
            return sut;
        }

        public static ElympicsLobbyClient InjectMockIAuthClient(this ElympicsLobbyClient sut, IAuthClient mock)
        {
            var authField = sut!.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == AuthClientFieldName);
            Assert.NotNull(authField);
            authField.SetValue(sut, mock);
            return sut;
        }

        public static ElympicsLobbyClient InjectRegionIAvailableRegionRetriever(this ElympicsLobbyClient sut, IAvailableRegionRetriever mock)
        {
            var availableRegionRetriever = sut.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == AvailableRegionRetriever);
            Assert.NotNull(availableRegionRetriever);
            availableRegionRetriever.SetValue(sut, mock);
            return sut;
        }

        public static ElympicsLobbyClient InjectIRoomManager(this ElympicsLobbyClient sut, IRoomsManager roomsManagerMock, IRoomsClient roomsClientMock)
        {
            var lazyRoomsManager = sut.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == RoomsManager);
            Assert.NotNull(lazyRoomsManager);
            var lazy = new Lazy<IRoomsManager>(roomsManagerMock);
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
