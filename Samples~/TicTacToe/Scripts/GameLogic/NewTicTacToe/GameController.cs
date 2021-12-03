using System.Collections;
using UnityEngine;
using Elympics;

namespace GameLogic.NewTicTacToe
{
	public class GameController : ElympicsMonoBehaviour
	{
		[SerializeField] private GameStateObject gameState = null;

		private int _occupiedFields = 0;

		private void Awake()
		{
			InitializeFields();
			AssignSides();
		}

		private void AssignSides()
		{
			var player1Side = (PlayerSide) Random.Range(1, 3);
			var player2Side = player1Side == PlayerSide.Circle ? PlayerSide.Cross : PlayerSide.Circle;
			gameState.SetSides(player1Side, player2Side);
			gameState.SetTurnForPlayer(player1Side == PlayerSide.Circle ? (int) ElympicsPlayer.FromIndex(0) : (int) ElympicsPlayer.FromIndex(1));
		}

		private void InitializeFields()
		{
			for (int i = 0; i < gameState.fields.Count; i++)
			{
				var field = gameState.fields[i];
				field.OwnershipChanged += FieldOwnershipChanged;
			}
		}

		private void FieldOwnershipChanged(Field field, PlayerSide playerSide)
		{
			int index = gameState.fields.FindIndex(field.Equals);
			Vector2Int coordinates = IndexToCoordinates(index);
			int length = LineLength(coordinates, playerSide);
			if (length == 3)
				GameOver(playerSide);
			_occupiedFields++;
			if (_occupiedFields == gameState.fields.Count)
				GameOver(PlayerSide.None);
		}

		private void GameOver(PlayerSide winner)
		{
			gameState.SetGameOver(true);

			if (Elympics.IsServer)
				StartCoroutine(EndGameOnServer(winner));
		}

		private IEnumerator EndGameOnServer(PlayerSide winner)
		{
			yield return new WaitForSeconds(3);

			var player1WinnerValue = winner == gameState.GetSide(0) ? 1 : 0;
			var player2WinnerValue = winner == gameState.GetSide(1) ? 1 : 0;

			Elympics.EndGame(new ResultMatchPlayerDatas
			{
				new ResultMatchPlayerData
				{
					GameEngineData = new[] {(byte) player1WinnerValue},
					MatchmakerData = new[] {(float) player1WinnerValue}
				},
				new ResultMatchPlayerData
				{
					GameEngineData = new[] {(byte) player2WinnerValue},
					MatchmakerData = new[] {(float) player2WinnerValue}
				}
			});
		}

		private Vector2Int IndexToCoordinates(int index) => new Vector2Int(index % 3, index / 3);

		private int CoordinatesToIndex(Vector2Int coordinates)
			=> AreCoordinatesValid(coordinates) ? coordinates.y * 3 + coordinates.x : -1;

		private bool AreCoordinatesValid(Vector2Int coordinates)
			=> coordinates.x >= 0 && coordinates.x < 3 && coordinates.y >= 0 && coordinates.y < 3;

		private int LineLength(Vector2Int coordinates, PlayerSide playerSide)
		{
			int maxLength = 1;
			for (int directionIndex = 0; directionIndex < 4; directionIndex++)
			{
				var delta = IndexToCoordinates(directionIndex) - Vector2Int.one;
				int leftLength = LineLengthInDirection(coordinates, playerSide, delta);
				int rightLength = LineLengthInDirection(coordinates, playerSide, -delta);
				maxLength = Mathf.Max(maxLength, leftLength + rightLength + 1);
			}

			return maxLength;
		}

		private int LineLengthInDirection(Vector2Int coordinates, PlayerSide playerSide, Vector2Int delta)
		{
			int length = -1;
			var coordinatesSide = playerSide;
			while (coordinatesSide == playerSide)
			{
				coordinates += delta;
				coordinatesSide = SideAtCoordinates(coordinates);
				length++;
			}

			return length;
		}

		private PlayerSide SideAtCoordinates(Vector2Int coordinates)
		{
			int index = CoordinatesToIndex(coordinates);
			if (index < 0 || index >= gameState.fields.Count)
				return PlayerSide.None;
			return gameState.fields[index].Ownership;
		}
	}
}
