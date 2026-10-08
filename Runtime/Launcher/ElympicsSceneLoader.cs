#nullable enable

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
    internal interface IElympicsSceneLoader
    {
        /// <summary>Returns false if Unity refused to start loading the scene.</summary>
        UniTask<bool> LoadSingleAsync(string scenePath);
        bool IsInBuild(string scenePath);
        string ActiveScenePath { get; }
        UniTask ReturnToEmptySceneAsync();
        UniTask ReleaseMemoryAsync(bool forceCollect);
    }

    internal sealed class ElympicsSceneLoader : IElympicsSceneLoader
    {
        public async UniTask<bool> LoadSingleAsync(string scenePath)
        {
            // LoadSceneAsync on a scene missing from Build Settings logs an error and returns null instead of throwing.
            var operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            if (operation == null)
                return false;
            await operation;
            return true;
        }

        public bool IsInBuild(string scenePath) =>
            !string.IsNullOrEmpty(scenePath) && SceneUtility.GetBuildIndexByScenePath(scenePath) >= 0;

        public string ActiveScenePath => SceneManager.GetActiveScene().path;

        internal const string EmptyBootSceneName = "ElympicsBoot";

        public async UniTask ReturnToEmptySceneAsync()
        {
            // Scene names must be unique, so reuse the generated scene if it survived an earlier return.
            var empty = SceneManager.GetSceneByName(EmptyBootSceneName);
            var created = !empty.IsValid() || !string.IsNullOrEmpty(empty.path);
            if (created)
                empty = SceneManager.CreateScene(EmptyBootSceneName);
            _ = SceneManager.SetActiveScene(empty);
            if (created)
                AddBlackCamera();

            var others = new List<Scene>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != empty)
                    others.Add(scene);
            }
            foreach (var scene in others)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if (operation != null)
                    await operation;
            }
        }

        // Without a camera, the screen keeps showing the last frame of the match.
        private static void AddBlackCamera()
        {
            var camera = new GameObject("ElympicsBootCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
        }

        public UniTask ReleaseMemoryAsync(bool forceCollect) => ElympicsMatchTeardown.ReleaseMemoryAsync(forceCollect);
    }
}
