#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Elympics.Editor.Tests
{
    /// <summary>Reuses the Test Runner's active untitled scene, otherwise opens an empty one additively. Dispose cleans up.</summary>
    internal sealed class AdditiveTestScene : IDisposable
    {
        private readonly List<GameObject> _created = new();
        private readonly bool _opened;
        private readonly bool _wasDirty;
        private readonly Scene _previousActive;

        public Scene Scene { get; }

        public AdditiveTestScene()
        {
            _previousActive = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(_previousActive.path))
            {
                Scene = _previousActive;
                _wasDirty = Scene.isDirty;
                return;
            }

            Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            _opened = true;
            _ = SceneManager.SetActiveScene(Scene);
        }

        public GameObject Create(string name, Transform? parent = null)
        {
            var gameObject = new GameObject(name);
            if (parent != null)
                gameObject.transform.SetParent(parent);
            else
            {
                SceneManager.MoveGameObjectToScene(gameObject, Scene);
                _created.Add(gameObject);
            }
            return gameObject;
        }

        public void Track(GameObject gameObject) => _created.Add(gameObject);

        // Not public API; without it the scene just stays dirty.
        private static void ClearDirtiness(Scene scene) => typeof(EditorSceneManager)
            .GetMethod("ClearSceneDirtiness", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(Scene) }, null)
            ?.Invoke(null, new object[] { scene });

        public void Dispose()
        {
            foreach (var gameObject in _created)
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);
            if (!_opened)
            {
                if (!_wasDirty && Scene.IsValid())
                    ClearDirtiness(Scene);
                return;
            }
            if (_previousActive.IsValid() && _previousActive.isLoaded)
                _ = SceneManager.SetActiveScene(_previousActive);
            if (Scene.IsValid())
                _ = EditorSceneManager.CloseScene(Scene, true);
        }
    }
}
