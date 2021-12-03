using UnityEngine;

namespace GameLogic.NewTicTacToe
{
	public class PlayerInputProvider : MonoBehaviour
	{
		[SerializeField] public GameStateObject gameState = null;

		private Input _lastClickedInput = Input.Empty;

		private void Start()
		{
			var fieldIndex = 0;
			foreach (var field in gameState.fields)
			{
				field.Index = fieldIndex++;
				field.Clicked += OnFieldClicked;
			}
		}

		private void OnFieldClicked(Field field) => _lastClickedInput = new Input(field.Index);

		public Input GetAndClearLastClickedInput()
		{
			var input = _lastClickedInput;
			_lastClickedInput = Input.Empty;
			return input;
		}
	}
}
