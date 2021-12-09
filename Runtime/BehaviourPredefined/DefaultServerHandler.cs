using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elympics
{
	public class DefaultServerHandler : ElympicsMonoBehaviour, IServerHandler
	{
		private          int                     _playersNumber;
		private readonly HashSet<ElympicsPlayer> _playersConnected = new HashSet<ElympicsPlayer>();

		private static readonly TimeSpan StartGameTimeout = TimeSpan.FromSeconds(30);
		private                 DateTime _waitToStartFinishTime;
		private                 bool     _gameStarted;

		public void OnServerInit(InitialMatchPlayerDatas initialMatchPlayerDatas)
		{
			if (!enabled)
				return;

			_playersNumber = initialMatchPlayerDatas.Count;
			var humansPlayers = initialMatchPlayerDatas.Count(x => !x.IsBot);
			Debug.Log($"Game initialized with {humansPlayers} human players and {initialMatchPlayerDatas.Count - humansPlayers} bots");

			StartCoroutine(WaitForGameStartOrEnd());
		}

		private IEnumerator WaitForGameStartOrEnd()
		{
			_waitToStartFinishTime = DateTime.Now + StartGameTimeout;

			while (DateTime.Now < _waitToStartFinishTime)
			{
				Debug.Log("Waiting for game to start");
				if (_gameStarted)
				{
					Debug.Log("Game started!");
					yield break;
				}

				yield return new WaitForSeconds(5);
			}

			Debug.Log("Forcing game end because game didn't start");
			Elympics.EndGame();
		}

		public void OnPlayerDisconnected(ElympicsPlayer player)
		{
			if (!enabled)
				return;

			Debug.Log($"Player {player} disconnected");
			Debug.Log("Game ended!");
			Elympics.EndGame();
		}

		public void OnPlayerConnected(ElympicsPlayer player)
		{
			if (!enabled)
				return;

			Debug.Log($"Player {player} connected");

			_playersConnected.Add(player);
			if (_playersConnected.Count != _playersNumber || _gameStarted)
				return;

			_gameStarted = true;
		}
	}
}