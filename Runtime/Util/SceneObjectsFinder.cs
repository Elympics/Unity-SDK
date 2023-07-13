using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
    public static class SceneObjectsFinder
    {
        public static List<T> FindObjectsOfTypeOnScene<T>(this GameObject gameObject, bool includeInactive = false)
        {
            return FindObjectsOfType<T>(gameObject.scene, includeInactive);
        }

        public static List<T> FindObjectsOfType<T>(int sceneIndex, bool includeInactive = false)
        {
            var activeScene = SceneManager.GetSceneAt(sceneIndex);
            return FindObjectsOfType<T>(activeScene, includeInactive);
        }

        public static List<T> FindObjectsOfType<T>(Scene scene, bool includeInactive = false)
        {
            var gameObjects = scene.GetRootGameObjects();
            var result = new List<T>();
            for (var i = 0; i < gameObjects.Length; i++)
            {
                var gameObject = gameObjects[i];
                result.AddRange(gameObject.GetComponentsInChildren<T>(includeInactive));
            }
            return result;
        }

        public static T GetComponentInRootGameObjects<T>(this GameObject gameObject)
        {
            return FindObjectOfTypeAtTopLevel<T>(gameObject.scene);
        }

        public static T FindObjectOfTypeAtTopLevel<T>(Scene scene)
        {
            var gameObjects = scene.GetRootGameObjects();
            for (var i = 0; i < gameObjects.Length; i++)
            {
                var gameObject = gameObjects[i];
                if (gameObject.TryGetComponent(out T result))
                    return result;
            }
            return default;
        }
    }
}
