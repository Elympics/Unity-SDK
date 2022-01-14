using UnityEditor;
using UnityEngine;

namespace Elympics
{
	public static class ElympicsMouseActions
	{
		private const string pathToElympicsSystem = "Elympics";

		[MenuItem(ElympicsEditorMenuPaths.MOUSE_ACTION_CREATE_ELYMPICS_SYSTEM, priority = 11)]
		private static void AddElympicsSystemToScene()
		{
			var elympicsSystemPrefabReference = Resources.Load<GameObject>(pathToElympicsSystem);

			if (elympicsSystemPrefabReference != null)
				PrefabUtility.InstantiatePrefab(elympicsSystemPrefabReference);
			else
				Debug.LogError("Cannot instantiate elympics system - prefab reference is null!");
		}
	}
}

