using Elympics;
using UnityEngine;

public class HelloWorldInputController : MonoBehaviour, IInputHandler
{
	[SerializeField]
	private GameObject[] labels = null;

	private bool _clicked;

	public void GetInputForClient(IInputWriter inputSerializer)
	{
		inputSerializer.Write(_clicked);
		_clicked = false;
	}

	public void GetInputForBot(IInputWriter inputSerializer)
	{
	}

	public void ApplyInput(ElympicsPlayer player, IInputReader inputReader)
	{
		inputReader.Read(out bool value);
		if (value)
			labels[(int) player].SetActive(value);
	}

	public void OnClick()
	{
		_clicked = true;
	}
}
