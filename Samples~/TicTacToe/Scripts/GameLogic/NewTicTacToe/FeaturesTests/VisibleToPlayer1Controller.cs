using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.NewTicTacToe
{
	public class VisibleToPlayer1Controller : MonoBehaviour
	{
		private                  int  _counter = 0;
		[SerializeField] private Text text = null;

		private void FixedUpdate()
		{
			_counter += 1;
			text.text = _counter.ToString();
		}
	}
}
