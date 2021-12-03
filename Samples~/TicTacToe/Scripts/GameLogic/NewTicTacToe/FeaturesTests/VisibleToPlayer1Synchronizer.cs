using UnityEngine;
using UnityEngine.UI;
using Elympics;

namespace GameLogic.NewTicTacToe
{
	[RequireComponent(typeof(Text))]
	public class VisibleToPlayer1Synchronizer : MonoBehaviour, IStateSerializationHandler
	{
		private Text _text;

		private readonly ElympicsString _message = new ElympicsString("Nothing");

		private void Awake()
		{
			_text = GetComponent<Text>();
		}

		public void OnPostStateDeserialize()
		{
			_text.text = _message.Value;
		}

		public void OnPreStateSerialize()
		{
			_message.Value = _text.text;
		}
	}
}
