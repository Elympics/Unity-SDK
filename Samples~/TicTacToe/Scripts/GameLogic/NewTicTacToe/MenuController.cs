using UnityEngine;
using UnityEngine.UI;
using Elympics;

public class MenuController : MonoBehaviour
{
	[SerializeField] private Button playButton = null;

	private void Start()
	{
		ElympicsLobbyClient.Instance.Authenticated += HandleAuthenticated;
		playButton.interactable = ElympicsLobbyClient.Instance.IsAuthenticated;
	}

	private void HandleAuthenticated(bool success, string userId, string jwtToken, string error)
	{
		playButton.interactable = success;
	}

	public void OnPlayClicked()
	{
		ElympicsLobbyClient.Instance.JoinMatch();
	}
}
