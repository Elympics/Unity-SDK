using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Elympics;
using Plugins.Elympics.Plugins.ParrelSync;

public class MenuController : MonoBehaviour
{
	[SerializeField] private Button     playButton;
	[SerializeField] private InputField halfRemotePlayerId;

	private const string PlayOnlineText = "Play Online";
	private const string CancelMatchmakingText = "Cancel matchmaking";

	private Text _playButtonText;
	private CancellationTokenSource _cts;

	private void Start()
	{
		ElympicsLobbyClient.Instance.Authenticated += HandleAuthenticated;
		playButton.interactable = ElympicsLobbyClient.Instance.IsAuthenticated;

		_playButtonText = playButton.GetComponentInChildren<Text>();
		_playButtonText.text = PlayOnlineText;

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
		if (_cts != null)
		{
			_cts.Cancel();
			_playButtonText.text = PlayOnlineText;
			_cts = null;
			return;
		}

		_cts = new CancellationTokenSource();
		_playButtonText.text = CancelMatchmakingText;
		ElympicsLobbyClient.Instance.PlayOnline(cancellationToken: _cts.Token);
	}
}
