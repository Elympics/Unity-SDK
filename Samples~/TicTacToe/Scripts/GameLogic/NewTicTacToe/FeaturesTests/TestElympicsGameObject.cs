using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Elympics;
using System;

public class TestElympicsGameObject : ElympicsMonoBehaviour, IUpdatable, IInitializable
{
	[SerializeField] private ElympicsBehaviour singleGameObjectReference = null;
	[SerializeField] private ElympicsBehaviour[] multipleGameObjectReferences = null;

	private readonly ElympicsInt _tick = new ElympicsInt();

	private ElympicsGameObject singleElympicsGameObject = new ElympicsGameObject(null);
	private ElympicsList<ElympicsGameObject> listWithElympicsGameObjects = new ElympicsList<ElympicsGameObject>(() => new ElympicsGameObject(null));

	public void Initialize()
	{
		foreach (ElympicsBehaviour behaviour in multipleGameObjectReferences)
		{
			listWithElympicsGameObjects.Add().Value = behaviour;
		}
	}

	public void ElympicsUpdate()
	{
		_tick.Value++;

		if (_tick % 200 == 80)
		{
			ChangeSingleGameObject();
			ChangePositionsInList();
		}
	}

	private void ChangePositionsInList()
	{
		ElympicsBehaviour firstElement = listWithElympicsGameObjects[0];

		for (int i = 1; i < listWithElympicsGameObjects.Count; i++)
		{
			listWithElympicsGameObjects[i - 1].Value = listWithElympicsGameObjects[i];
		}

		listWithElympicsGameObjects[listWithElympicsGameObjects.Count - 1].Value = firstElement;
	}

	//private void Update()
	//{
	//	if (Input.GetKeyDown(KeyCode.P))
	//	{
	//		foreach (ElympicsBehaviour elympicsBehaviour in listWithElympicsGameObjects)
	//		{
	//			if (elympicsBehaviour != null)
	//				Debug.Log(elympicsBehaviour.gameObject.name);
	//			else
	//				Debug.Log("NULL");
	//		}
	//	}
	//}

	private void ChangeSingleGameObject()
	{
		if (singleElympicsGameObject.Value == null)
			singleElympicsGameObject.Value = singleGameObjectReference;
		else
			singleElympicsGameObject.Value = null;
	}
}
