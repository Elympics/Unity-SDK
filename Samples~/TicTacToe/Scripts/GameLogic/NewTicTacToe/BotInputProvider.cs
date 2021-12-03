using UnityEngine;

namespace GameLogic.NewTicTacToe
{
	public class BotInputProvider : MonoBehaviour
	{
		public int             playerId;
		public GameStateObject gameState;

		public Input ChosenField = Input.Empty;

		public void FixedUpdate()
		{
			ChosenField = Input.Empty;
			var myPlayerSide = gameState.GetSide(playerId);
			if (myPlayerSide == PlayerSide.None)
				return;

			if (!gameState.IsPlayersTurn(playerId))
				return;

			foreach (var field in gameState.fields)
			{
				if (field.Ownership != PlayerSide.None)
					continue;
				ChosenField = new Input(field.Index);
				break;
			}
		}
	}
}
