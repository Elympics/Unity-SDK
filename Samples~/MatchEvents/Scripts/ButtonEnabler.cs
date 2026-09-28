using System;
using Elympics;
using Elympics.Communication.Models.Public;
using MatchTcpClients.Synchronizer;
using UnityEngine;
using UnityEngine.UI;

namespace MatchEvents
{
    public class ButtonEnabler : MonoBehaviour, IClientHandlerGuid
    {
#pragma warning disable CS0649  // Field is never assigned to, and will always have its default value null

        [SerializeField] private Button playerConnectButton;
        [SerializeField] private Button spectatorConnectButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private Button endGameButton;

        [SerializeField] private GameLifecycleTester gameLifecycleTester;

#pragma warning restore CS0649

        public void OnStandaloneClientInit(InitialMatchPlayerDataGuid data) => OnClientInit();
        public void OnClientsOnServerInit(InitialMatchPlayerDatasGuid data) => OnClientInit();

        private void OnClientInit()
        {
            SetButtonsDisconnected();

            gameLifecycleTester.ConnectingStarted += DisableAllButtons;
            gameLifecycleTester.ConnectingFinished += success =>
            {
                if (success)
                    SetButtonsConnected();
                else
                    SetButtonsDisconnected();
            };
        }

        public void OnConnected(TimeSynchronizationData data) => SetButtonsConnected();
        public void OnAuthenticated(Guid userId) => SetButtonsConnected();
        public void OnMatchJoined(MatchInitialData matchInitialData) => SetButtonsConnected();

        public void OnConnectingFailed() => SetButtonsDisconnected();
        public void OnAuthenticatedFailed(string errorMessage) => SetButtonsDisconnected();
        public void OnMatchJoinedFailed(string errorMessage) => SetButtonsDisconnected();
        public void OnDisconnectedByClient() => SetButtonsDisconnected();

        public void OnMatchEnded(Guid matchId) => DisableAllButtons();
        public void OnDisconnectedByServer() => DisableAllButtons();

        private void DisableAllButtons()
        {
            playerConnectButton.interactable = false;
            spectatorConnectButton.interactable = false;
            disconnectButton.interactable = false;
            endGameButton.interactable = false;
        }

        private void SetButtonsConnected()
        {
            playerConnectButton.interactable = false;
            spectatorConnectButton.interactable = false;
            disconnectButton.interactable = true;
            endGameButton.interactable = true;
        }

        private void SetButtonsDisconnected()
        {
            playerConnectButton.interactable = true;
            spectatorConnectButton.interactable = true;
            disconnectButton.interactable = false;
            endGameButton.interactable = false;
        }
    }
}
