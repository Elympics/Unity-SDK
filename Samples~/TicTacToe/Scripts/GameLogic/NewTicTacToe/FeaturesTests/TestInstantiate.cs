using UnityEngine;
using Elympics;

public class TestInstantiate : ElympicsMonoBehaviour, IUpdatable
{
	private readonly ElympicsInt _tick = new ElympicsInt();
	private GameObject _cube;

	public void ElympicsUpdate()
	{
		_tick.Value++;
		if (_tick.Value % 200 == 80)
			_cube = ElympicsInstantiate("Cube", ElympicsPlayer.All);

		if (_tick.Value % 200 == 199 && _cube != null)
		{
			ElympicsDestroy(_cube);
			_cube = null;
		}
	}
}
