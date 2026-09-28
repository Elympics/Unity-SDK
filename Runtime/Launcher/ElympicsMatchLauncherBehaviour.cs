#nullable enable

using Elympics.Core.Logger;
using UnityEngine;

namespace Elympics
{
    /// <summary>
    /// Persistent carrier of <see cref="ElympicsMatchLauncher"/>. Created before the first scene loads,
    /// so the boot scene needs no setup at all.
    /// </summary>
    [DefaultExecutionOrder(ElympicsExecutionOrder.MatchLauncher)]
    [AddComponentMenu("")]
    internal sealed class ElympicsMatchLauncherBehaviour : MonoBehaviour
    {
        internal const string GameObjectName = "ElympicsMatchLauncher";

#if (UNITY_WEBGL || UNITY_IOS) && !UNITY_EDITOR
        private const bool PlatformCanListen = false;
#else
        private const bool PlatformCanListen = true;
#endif

        private ElympicsMatchLauncher? _launcher;

        /// <summary>
        /// True in builds that run a game server (online or half-remote). There the launcher stays passive:
        /// no host bridge and no <c>ready</c> event.
        /// </summary>
        private static bool IsServerProcess => ApplicationParameters.ShouldLoadElympicsOnlineServer || (ScriptingSymbols.IsUnityServer && !Application.isEditor);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (ElympicsMatchLauncher.Current != null)
                return;

            // Endpoints in ElympicsConfig read these, so initialize them before any scene code runs.
            if (!ApplicationParameters.InitializeParameters())
                ElympicsLogger.LogError("Could not initialize application parameters.");

            var gameObject = new GameObject(GameObjectName);
            DontDestroyOnLoad(gameObject);
            _ = gameObject.AddComponent<ElympicsMatchLauncherBehaviour>();
        }

        private void Awake()
        {
            _launcher = new ElympicsMatchLauncher(new ElympicsSceneLoader(), ElympicsConfig.Load, PlatformCanListen, _ => ExitUtility.ExitGame(), Debug.isDebugBuild);
            ElympicsMatchLauncher.Current = _launcher;
        }

        private void Start()
        {
            // Reading ElympicsConfig is deferred to Start, as Resources are not guaranteed to be loadable at BeforeSceneLoad on every platform.
            if (_launcher == null || IsServerProcess)
                return;
            _ = _launcher.BootScene;
            _launcher.EmitReady();
        }

        private void OnDestroy()
        {
            if (ElympicsMatchLauncher.Current == _launcher)
                ElympicsMatchLauncher.Current = null;
        }
    }
}
