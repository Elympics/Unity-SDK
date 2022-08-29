using Elympics;
using MatchTcpClients.Synchronizer;
using UnityEngine;
using UnityEngine.UI;

namespace MatchEvents
{
	public class ButtonEnabler : MonoBehaviour, IClientHandler
	{
		#pragma warning disable CS0649  // Field is never assigned to, and will always have its default value null

		[SerializeField] private Button playerConnectButton;
		[SerializeField] private Button spectatorConnectButton;
		[SerializeField] private Button disconnectButton;
		[SerializeField] private Button endGameButton;

		[SerializeField] private GameLifecycleTester gameLifecycleTester;
		[SerializeField] private AsyncEventsDispatcher asyncEventsDispatcher;

		#pragma warning restore CS0649

		public void OnStandaloneClientInit(InitialMatchPlayerData data) => OnClientInit();
		public void OnClientsOnServerInit(InitialMatchPlayerDatas data) => OnClientInit();

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
		public void OnAuthenticated(string userId) => SetButtonsConnected();
		public void OnMatchJoined(string matchId) => SetButtonsConnected();

		public void OnConnectingFailed() => SetButtonsDisconnected();
		public void OnAuthenticatedFailed(string errorMessage) => SetButtonsDisconnected();
		public void OnMatchJoinedFailed(string errorMessage) => SetButtonsDisconnected();
		public void OnDisconnectedByClient() => SetButtonsDisconnected();

		public void OnMatchEnded(string matchId) => DisableAllButtons();
		public void OnDisconnectedByServer() => DisableAllButtons();

		private void DisableAllButtons()
		{
			asyncEventsDispatcher.Enqueue(() =>
			{
				playerConnectButton.interactable = false;
				spectatorConnectButton.interactable = false;
				disconnectButton.interactable = false;
				endGameButton.interactable = false;
			});
		}

		private void SetButtonsConnected()
		{
			asyncEventsDispatcher.Enqueue(() =>
			{
				playerConnectButton.interactable = false;
				spectatorConnectButton.interactable = false;
				disconnectButton.interactable = true;
				endGameButton.interactable = true;
			});
		}

		private void SetButtonsDisconnected()
		{
			asyncEventsDispatcher.Enqueue(() =>
			{
				playerConnectButton.interactable = true;
				spectatorConnectButton.interactable = true;
				disconnectButton.interactable = false;
				endGameButton.interactable = false;
			});
		}

		public void OnSynchronized(TimeSynchronizationData data)
		{ }
	}
}
