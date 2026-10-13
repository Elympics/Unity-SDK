using System;
using System.Collections.Generic;
using Elympics.Cysharp.Threading.Tasks;
using Elympics;
using JetBrains.Annotations;
using MatchTcpClients.Synchronizer;

namespace MatchEvents
{
    public class GameLifecycleTester : ElympicsMonoBehaviour, IClientHandlerGuid, IServerHandlerGuid, IBotHandlerGuid
    {
        public event Action ConnectingStarted;
        public event Action<bool> ConnectingFinished;

        [UsedImplicitly]
        public void OnEndGameClicked() => EndGameInputHandler.ShouldGameEnd = true;

        [UsedImplicitly]
        public async UniTaskVoid OnConnectAndJoinAsPlayerClicked()
        {
            ConnectingStarted?.Invoke();
            try
            {
                await Elympics.ConnectAndJoinAsPlayerAsync();
                ConnectingFinished?.Invoke(true);
            }
            catch
            {
                ConnectingFinished?.Invoke(false);
            }
        }

        [UsedImplicitly]
        public async UniTaskVoid OnConnectAndJoinAsSpectatorClicked()
        {
            ConnectingStarted?.Invoke();
            try
            {
                await Elympics.ConnectAndJoinAsSpectatorAsync();
                ConnectingFinished?.Invoke(true);
            }
            catch
            {
                ConnectingFinished?.Invoke(false);
            }
        }

        [UsedImplicitly]
        public void OnDisconnectClicked() => Elympics.Disconnect();

        #region IClientHandler

        public void OnStandaloneClientInit(InitialMatchPlayerDataGuid data) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(data), data }
            });

        public void OnClientsOnServerInit(InitialMatchPlayerDatasGuid datas) => Serializer.PrintCall(
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

        public void OnAuthenticated(Guid userId) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(userId), userId }
            });

        public void OnAuthenticatedFailed(string errorMessage) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(errorMessage), errorMessage }
            });

        public void OnMatchJoinedFailed(string errorMessage) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(errorMessage), errorMessage }
            });

        public void OnMatchEnded(Guid matchId) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(matchId), matchId }
            });

        #endregion IClientHandler

        #region IServerHandler

        public void OnServerInit(InitialMatchPlayerDatasGuid datas) => Serializer.PrintCall(
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

        public void OnBotsOnServerInit(InitialMatchPlayerDatasGuid datas) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(datas), datas }
            });

        #endregion IBotHandler
    }
}
