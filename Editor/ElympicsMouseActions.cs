using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
    public static class ElympicsMouseActions
    {
        private const string PathToElympicsSystem = "Elympics";
        private const string PathToElympicsLobby = "ElympicsLobby";

        [MenuItem(ElympicsEditorMenuPaths.MOUSE_ACTION_CREATE_ELYMPICS_SYSTEM)]
        private static void AddElympicsSystemToScene()
        {
            const string name = "Elympics System";
            if (HasActiveSceneAnyObjectOfType<ElympicsLobbyClient>())
            {
                ElympicsLogger.LogError($"{name} cannot be placed in the menu scene. Use a separate game scene.");
                return;
            }
            AddUniquePrefabToScene<GameSceneManager>(PathToElympicsSystem, name);
        }

        [MenuItem(ElympicsEditorMenuPaths.MOUSE_ACTION_CREATE_ELYMPICS_LOBBY)]
        private static void AddElympicsLobbyToScene()
        {
            const string name = "Elympics Lobby";
            if (HasActiveSceneAnyObjectOfType<GameSceneManager>())
            {
                ElympicsLogger.LogError($"{name} cannot be placed in the game scene. Use a separate menu scene.");
                return;
            }
            AddUniquePrefabToScene<ElympicsLobbyClient>(PathToElympicsLobby, name);
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

