using UnityEngine;
using Elympics;

public class TestInstantiate : ElympicsMonoBehaviour, IUpdatable
{
	//private void Start()
	//{
	//	StartCoroutine(SpawnCoroutine());
	//}
	
	//private IEnumerator SpawnCoroutine()
	//{
	//	yield return new WaitForSeconds(3);
	//	var cube = ElympicsInstantiate("Cube");
	//	cube.transform.position = new Vector3(1, 2, 3);
	//	// cube.GetComponentInChildren<SphereCollider>().transform.position = new Vector3(0.75f, 0.5f, 0);
	//	yield return new WaitForSeconds(3);
	//	ElympicsDestroy(cube);
	//}


	private readonly ElympicsInt _tick = new ElympicsInt();
	private GameObject _cube;

	public void ElympicsUpdate()
	{
		_tick.Value++;
		if (_tick % 200 == 80)
			_cube = ElympicsInstantiate("Cube", ElympicsPlayer.All);

		if (_tick % 200 == 199 && _cube != null)
		{
			ElympicsDestroy(_cube);
			_cube = null;
		}
	}
}
