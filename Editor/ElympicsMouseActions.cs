using Elympics.Core.Logger;
using Elympics.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
    public static class ElympicsMouseActions
    {
        private const string PathToElympicsSystem = "Elympics";

        [MenuItem(ElympicsEditorMenuPaths.MouseActionCreateElympicsSystem)]
        private static void AddElympicsSystemToScene()
        {
            const string name = "Elympics System";
            AddUniquePrefabToScene<GameSceneManager>(PathToElympicsSystem, name);
        }

        private static void AddUniquePrefabToScene<T>(string path, string name)
            where T : Component
        {
            if (HasActiveSceneAnyObjectOfType<T>())
            {
                ElympicsLogger.LogError($"{name} is already present in the current scene.");
                return;
            }
            var prefabReference = Resources.Load<T>(path);
            if (prefabReference == null)
            {
                ElympicsLogger.LogError($"Cannot instantiate {name} - prefab reference is null!");
                return;
            }
            var instance = PrefabUtility.InstantiatePrefab(prefabReference.gameObject);
            Undo.RegisterCreatedObjectUndo(instance, $"Instantiate {name}");
        }

        private static bool HasActiveSceneAnyObjectOfType<T>() where T : Object =>
            SceneObjectsFinder.FindObjectsOfType<T>(SceneManager.GetActiveScene(), true).Count > 0;
    }
}

