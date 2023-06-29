using System.Collections.Generic;
using UnityEngine;
using Elympics;

namespace GameLogic.NewTicTacToe
{
	public class GameStateObject : ElympicsMonoBehaviour
	{
		[SerializeField] private GameObject            gameOverObject;
		[SerializeField] private AsyncEventsDispatcher asyncEventsDispatcher;

		#region GameState

		[SerializeField] public  List<Field>  fields;
		[SerializeField] private ElympicsInt  player1Side       = new ElympicsInt(0);
		[SerializeField] private ElympicsInt  player2Side       = new ElympicsInt(0);
		[SerializeField] private ElympicsInt  currentPlayerTurn = new ElympicsInt((int) ElympicsPlayer.Invalid);
		[SerializeField] private ElympicsBool gameOver          = new ElympicsBool(false);

		#endregion

		private void Awake()
		{
			gameOver.ValueChanged += HandleGameOverChanged;
		}

		private void HandleGameOverChanged(bool lastValue, bool newValue)
		{
			asyncEventsDispatcher.Enqueue(() => gameOverObject.SetActive(newValue));
		}

		public void SetSides(PlayerSide first, PlayerSide second)
		{
			player1Side.Value = (int)first;
			player2Side.Value = (int)second;
		}

		public PlayerSide GetSide(int playerId)
		{
			if (playerId == (int) ElympicsPlayer.FromIndex(0))
				return (PlayerSide) player1Side.Value;
			if (playerId == (int) ElympicsPlayer.FromIndex(1))
				return (PlayerSide) player2Side.Value;
			return PlayerSide.None;
		}

		public bool IsPlayersTurn(int playerType) => playerType == currentPlayerTurn.Value;

		public void SetTurnForPlayer(int playerId) => currentPlayerTurn.Value = playerId;

		public void ChangeTurn() => currentPlayerTurn.Value = (int) ElympicsPlayer.FromIndex(0) + 1 - currentPlayerTurn.Value;

		public void SetGameOver(bool value) => gameOver.Value = value;

		public bool IsGameOver() => gameOver.Value;
	}
}
