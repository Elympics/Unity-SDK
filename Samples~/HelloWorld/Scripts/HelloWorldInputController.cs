using Elympics;
using UnityEngine;

public class HelloWorldInputController : ElympicsMonoBehaviour, IInputHandler, IUpdatable
{
	[SerializeField]
	private GameObject[] labels = null;

	private bool _clicked;

	public void OnInputForClient(IInputWriter inputSerializer)
	{
		inputSerializer.Write(_clicked);
		_clicked = false;
	}

	public void OnInputForBot(IInputWriter inputSerializer)
	{
	}

	public void ElympicsUpdate()
	{
		for (var playerId = 0; playerId < labels.Length; playerId++)
		{
			if (!ElympicsBehaviour.TryGetInput(ElympicsPlayer.FromIndex(playerId), out var inputReader))
				continue;
			inputReader.Read(out bool value);
			if (value)
				labels[playerId].SetActive(value);
		}
	}

	public void OnClick()
	{
		_clicked = true;
	}
}
