using UnityEditor;
using UnityEngine;

namespace Elympics
{
    public static class ElympicsMouseActions
    {
        private const string PathToElympicsSystem = "Elympics";

        [MenuItem(ElympicsEditorMenuPaths.MOUSE_ACTION_CREATE_ELYMPICS_SYSTEM, priority = 11)]
        private static void AddElympicsSystemToScene()
        {
            var elympicsSystemPrefabReference = Resources.Load<GameObject>(PathToElympicsSystem);

            if (elympicsSystemPrefabReference != null)
                _ = PrefabUtility.InstantiatePrefab(elympicsSystemPrefabReference);
            else
                Debug.LogError("Cannot instantiate elympics system - prefab reference is null!");
        }
    }
}

