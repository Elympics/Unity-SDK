#nullable enable

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Elympics
{
    /// <summary>Scene and memory operations of the match launcher, replaceable in tests.</summary>
    internal interface IElympicsSceneLoader
    {
        /// <summary>Loads <paramref name="scenePath"/> in <see cref="LoadSceneMode.Single"/>. Returns false if Unity refused to start loading it.</summary>
        UniTask<bool> LoadSingleAsync(string scenePath);
        bool IsInBuild(string scenePath);
        string ActiveScenePath { get; }
        /// <summary>Makes a generated empty scene the only loaded scene. Used when no usable boot scene is configured.</summary>
        UniTask ReturnToEmptySceneAsync();
        UniTask ReleaseMemoryAsync();
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
            if (!empty.IsValid() || !string.IsNullOrEmpty(empty.path))
                empty = SceneManager.CreateScene(EmptyBootSceneName);
            _ = SceneManager.SetActiveScene(empty);

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

        public UniTask ReleaseMemoryAsync() => ElympicsMatchTeardown.ReleaseMemoryAsync();
    }
}
