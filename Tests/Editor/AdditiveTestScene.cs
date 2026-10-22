using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Elympics.Editor.Tests
{
    /// <summary>
    /// A scene for test objects that never replaces or dirties the developer's scenes. The Test Runner already runs EditMode tests
    /// in its own untitled scene (and can't add another untitled one), so that scene is used when it is active; otherwise an empty
    /// scene is opened additively. Created objects are destroyed and an opened scene is closed on dispose.
    /// </summary>
    internal sealed class AdditiveTestScene : IDisposable
    {
        private readonly List<GameObject> _created = new();
        private readonly bool _opened;
        private readonly Scene _previousActive;

        public Scene Scene { get; }

        public AdditiveTestScene()
        {
            _previousActive = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(_previousActive.path))
            {
                Scene = _previousActive;
                return;
            }

            Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            _opened = true;
            _ = SceneManager.SetActiveScene(Scene);
        }

        public GameObject Create(string name, Transform parent = null)
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

        /// <summary>Registers a root object created elsewhere (e.g. an instantiated prefab) for cleanup.</summary>
        public void Track(GameObject gameObject) => _created.Add(gameObject);

        public void Dispose()
        {
            foreach (var gameObject in _created)
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);
            if (!_opened)
                return;
            if (_previousActive.IsValid() && _previousActive.isLoaded)
                _ = SceneManager.SetActiveScene(_previousActive);
            if (Scene.IsValid())
                _ = EditorSceneManager.CloseScene(Scene, true);
        }
    }
}
