using UnityEngine;
using UnityEngine.UI;
using Elympics;
using Elympics.Models.Authentication;
using Plugins.Elympics.Plugins.ParrelSync;

namespace MatchEvents
{
	public class MenuController : MonoBehaviour
	{
		[SerializeField] private Button playButton;
		[SerializeField] private InputField halfRemotePlayerId;

		private void Start()
		{
			ElympicsLobbyClient.Instance.AuthenticatedGuid += HandleAuthenticated;
			playButton.interactable = ElympicsLobbyClient.Instance.IsAuthenticatedWith(AuthType.ClientSecret);

			if (!ElympicsClonesManager.IsClone())
				return;
			halfRemotePlayerId.text = ElympicsGameConfig.GetHalfRemotePlayerIndex(0).ToString();
			halfRemotePlayerId.placeholder.GetComponent<Text>().enabled = true;
		}

		private void HandleAuthenticated(Result<AuthenticationData, string> result)
		{
			playButton.interactable = result.IsSuccess;
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
