#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
    public static class SceneObjectsFinder
    {
        public static List<T> FindObjectsOfTypeOnScene<T>(this GameObject gameObject, bool includeInactive = false) =>
            FindObjectsOfType<T>(gameObject.scene, includeInactive);

        public static List<T> FindObjectsOfType<T>(int sceneIndex, bool includeInactive = false) =>
            FindObjectsOfType<T>(SceneManager.GetSceneAt(sceneIndex), includeInactive);

        public static List<T> FindObjectsOfType<T>(Scene scene, bool includeInactive = false) =>
            scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(includeInactive)).ToList();

        public static T? GetComponentInRootGameObjects<T>(this GameObject gameObject) =>
            FindObjectOfTypeAtTopLevel<T>(gameObject.scene);

        public static T? FindObjectOfTypeAtTopLevel<T>(Scene scene) =>
            scene.GetRootGameObjects().Select(go => go.GetComponent<T>()).FirstOrDefault();
    }
}
