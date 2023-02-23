using System;
using System.Collections.Generic;
using Elympics;
using JetBrains.Annotations;
using MatchTcpClients.Synchronizer;

namespace MatchEvents
{
    public class GameLifecycleTester : ElympicsMonoBehaviour, IClientHandler, IServerHandler, IBotHandler
    {
        public event Action ConnectingStarted;
        public event Action<bool> ConnectingFinished;

        [UsedImplicitly]
        public void OnEndGameClicked()
        {
            EndGameInputHandler.ShouldGameEnd = true;
        }

        [UsedImplicitly]
        public void OnConnectAndJoinAsPlayerClicked()
        {
            void OnConnectAndJoinAsPlayerResult(bool success)
            {
                Serializer.PrintCall(new Dictionary<string, object>
                {
                    { nameof(success), success }
                }, $"{nameof(Elympics.ConnectAndJoinAsPlayer)} callback");
                ConnectingFinished?.Invoke(success);
            }

            ConnectingStarted?.Invoke();
            Elympics.ConnectAndJoinAsPlayer(OnConnectAndJoinAsPlayerResult);
        }

        [UsedImplicitly]
        public void OnConnectAndJoinAsSpectatorClicked()
        {
            void OnConnectAndJoinAsSpectatorResult(bool success)
            {
                Serializer.PrintCall(new Dictionary<string, object>
                {
                    { nameof(success), success }
                }, $"{nameof(Elympics.ConnectAndJoinAsSpectator)} callback");
                ConnectingFinished?.Invoke(success);
            }

            ConnectingStarted?.Invoke();
            StartCoroutine(Elympics.ConnectAndJoinAsSpectator(OnConnectAndJoinAsSpectatorResult));
        }

        [UsedImplicitly]
        public void OnDisconnectClicked()
        {
            Elympics.Disconnect();
        }

        #region IClientHandler

        public void OnStandaloneClientInit(InitialMatchPlayerData data) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(data), data }
            });

        public void OnClientsOnServerInit(InitialMatchPlayerDatas datas) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(datas), datas }
            });

        public void OnConnected(TimeSynchronizationData data) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(data), data }
            });

        public void OnConnectingFailed() => Serializer.PrintCall();
        public void OnDisconnectedByServer() => Serializer.PrintCall();
        public void OnDisconnectedByClient() => Serializer.PrintCall();

        public void OnSynchronized(TimeSynchronizationData data) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(data), data }
            });

        public void OnAuthenticated(string userId) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(userId), userId }
            });

        public void OnAuthenticatedFailed(string errorMessage) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(errorMessage), errorMessage }
            });

        public void OnMatchJoined(string matchId) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(matchId), matchId }
            });

        public void OnMatchJoinedFailed(string errorMessage) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(errorMessage), errorMessage }
            });

        public void OnMatchEnded(string matchId) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(matchId), matchId }
            });

        #endregion IClientHandler

        #region IServerHandler

        public void OnServerInit(InitialMatchPlayerDatas datas) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(datas), datas }
            });

        public void OnPlayerDisconnected(ElympicsPlayer player) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(player), player }
            });

        public void OnPlayerConnected(ElympicsPlayer player) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(player), player }
            });

        #endregion IServerHandler

        #region IBotHandler

        public void OnStandaloneBotInit(InitialMatchPlayerData data) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(data), data }
            });

        public void OnBotsOnServerInit(InitialMatchPlayerDatas datas) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(datas), datas }
            });

        #endregion IBotHandler
    }
}
