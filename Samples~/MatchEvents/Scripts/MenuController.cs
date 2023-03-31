using System;
using UnityEngine;
using UnityEngine.UI;
using Elympics;
using Plugins.Elympics.Plugins.ParrelSync;

namespace MatchEvents
{
	public class MenuController : MonoBehaviour
	{
		[SerializeField] private Button playButton;
		[SerializeField] private InputField halfRemotePlayerId;

		private void Start()
		{
			ElympicsLobbyClient.Instance.Authenticated += HandleAuthenticated;
			playButton.interactable = ElympicsLobbyClient.Instance.IsAuthenticated;

			if (!ElympicsClonesManager.IsClone())
				return;
			halfRemotePlayerId.text = ElympicsGameConfig.GetHalfRemotePlayerIndex(0).ToString();
			halfRemotePlayerId.placeholder.GetComponent<Text>().enabled = true;
		}

		private void HandleAuthenticated(bool success, Guid userId, string jwtToken, string error)
		{
			playButton.interactable = success;
		}

		public void OnPlayLocalClicked()
		{
			ElympicsLobbyClient.Instance.PlayOffline();
		}

		public void OnPlayHalfRemoteClicked()
		{
			var playerId = int.Parse(halfRemotePlayerId.text);
			ElympicsLobbyClient.Instance.PlayHalfRemote(playerId);
		}

		public void OnStartHalfRemoteServer()
		{
			ElympicsLobbyClient.Instance.StartHalfRemoteServer();
		}

		public void OnPlayOnlineClicked()
		{
			ElympicsLobbyClient.Instance.PlayOnline();
		}
	}
}
