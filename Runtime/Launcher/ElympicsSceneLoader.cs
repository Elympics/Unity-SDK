#nullable enable

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

        public UniTask ReleaseMemoryAsync() => ElympicsMatchTeardown.ReleaseMemoryAsync();
    }
}
