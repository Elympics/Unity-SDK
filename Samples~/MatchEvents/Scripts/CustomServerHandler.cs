using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Elympics;
using UnityEngine;
using UnityEngine.UI;

namespace MatchEvents
{
	[RequireComponent(typeof(Text))]
	public class CustomServerHandler : ElympicsMonoBehaviour, IServerHandler, IInitializable
	{
		[SerializeField] private int startGameTimeout = 30;

		private Text _text;
		private bool _gameStarted;
		private int _totalPlayers;
		private int _totalBotPlayers;

		private readonly Dictionary<ElympicsPlayer, bool> _isBot = new Dictionary<ElympicsPlayer, bool>();
		private readonly HashSet<ElympicsPlayer> _playersConnected = new HashSet<ElympicsPlayer>();
		private readonly HashSet<ElympicsPlayer> _botPlayersConnected = new HashSet<ElympicsPlayer>();

		public void Initialize()
		{
			_text = GetComponent<Text>();
			text.ValueChanged += (_, newValue) => _text.text = newValue;
		}

		public void OnServerInit(InitialMatchPlayerDatas initialMatchPlayerDatas)
		{
			foreach (var data in initialMatchPlayerDatas)
				_isBot[data.Player] = data.IsBot;

			_totalPlayers = initialMatchPlayerDatas.Count;
			var totalHumanPlayers = initialMatchPlayerDatas.Count(x => !x.IsBot);
			_totalBotPlayers = _totalPlayers - totalHumanPlayers;
			Debug.Log($"Game initialized with {totalHumanPlayers} human players and {_totalBotPlayers} bots");

			StartCoroutine(WaitForGameStartOrEnd());
		}

		private IEnumerator WaitForGameStartOrEnd()
		{
			var waitToStartFinishTime = DateTime.Now + TimeSpan.FromSeconds(startGameTimeout);

			while (DateTime.Now < waitToStartFinishTime)
			{
				if (_gameStarted)
					yield break;

				Debug.Log("Waiting for game to start");
				yield return new WaitForSeconds(5);
			}

			Debug.Log("Forcing game end because game didn't start");
			Elympics.EndGame();
		}

		public void OnPlayerDisconnected(ElympicsPlayer player)
		{
			Debug.Log($"Player {player} disconnected");

			_playersConnected.Remove(player);
			if (_isBot[player])
				_botPlayersConnected.Remove(player);
			UpdateText();
			if (_playersConnected.Count > _totalBotPlayers || !_gameStarted)
				return;

			Debug.Log("No more players connected, ending the game");
			Elympics.EndGame();
		}

		public void OnPlayerConnected(ElympicsPlayer player)
		{
			Debug.Log($"Player {player} connected");

			_playersConnected.Add(player);
			if (_isBot[player])
				_botPlayersConnected.Add(player);
			UpdateText();
			if (_playersConnected.Count != _totalPlayers || _gameStarted)
				return;

			_gameStarted = true;
			Debug.Log("Game started!");
		}

		private void UpdateText()
		{
			text.Value = $"Connected players: {_playersConnected.Count}\n(including {_botPlayersConnected.Count} bots)";
			test.Value++;
		}
	}
}
