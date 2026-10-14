#nullable enable

using Elympics.Core.Logger;
using Elympics.Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Elympics
{
    internal static class ElympicsMatchLauncherBootstrap
    {
        private static IElympicsHostBridge? bridge;
        private static BridgePoller? poller;

        /// <summary>
        /// A process that runs one match without a host gets no host bridge and no <c>ready</c> event.
        /// Other server builds (UNITY_SERVER) are driven by a host like any other build.
        /// </summary>
        internal static bool IsPassive => ApplicationParameters.ShouldLoadElympicsOnlineServer || LaunchDataFromParameters.IsSet;

        // Keeps the statics correct with domain reload disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Shutdown();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Create()
        {
            if (ElympicsMatchLauncher.Current != null)
                return;

            // Endpoints in ElympicsConfig read these, so initialize them before any scene code runs.
            if (!ApplicationParameters.InitializeParameters())
                ElympicsLogger.LogError("Could not initialize application parameters.");

            ElympicsMatchLauncher.Current = new DefaultElympicsMatchLauncher(new ElympicsSceneLoader(), ElympicsConfig.Load, ElympicsLaunchPlatform.Current, _ => ExitUtility.ExitGame(), Debug.isDebugBuild && !Application.isEditor,
                LaunchDataFromParameters.IsSet);
            Application.quitting -= Shutdown;
            Application.quitting += Shutdown;
        }

        // Reading ElympicsConfig waits for AfterSceneLoad, as Resources are not guaranteed to be loadable at BeforeSceneLoad on every platform.
        // UniTask's player loop runners are also ready only after its own BeforeSceneLoad hook.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            var launcher = ElympicsMatchLauncher.Current;
            if (launcher == null || bridge != null)
                return;
            if (IsPassive)
            {
#if UNITY_EDITOR
                LoadGameplaySceneForVirtualPlayer();
#endif
                return;
            }
            StartBridge(launcher);
            launcher.EmitReady();
        }

#if UNITY_EDITOR
        /// <summary>Multiplayer Play Mode gives virtual players the scenes open in the main Editor, e.g. the boot scene of a Match Launcher launch.
        /// The launch mode parameter starts a match only in the gameplay scene, so a virtual player opens it itself.</summary>
        private static void LoadGameplaySceneForVirtualPlayer()
        {
            var gameplayScene = ElympicsConfig.LoadCurrentElympicsGameConfig()?.GameplayScene;
            if (!ShouldLoadGameplayScene(PlayModeInstance.IsVirtualPlayer, LaunchDataFromParameters.IsSet, gameplayScene, SceneManager.GetActiveScene().path))
                return;
            ElympicsLogger.LogInfo($"Loading the gameplay scene {gameplayScene} for the launch mode parameter of this virtual player.");
            // Unlike SceneManager, this loads scenes missing from Build Settings.
            _ = EditorSceneManager.LoadSceneAsyncInPlayMode(gameplayScene, new LoadSceneParameters(LoadSceneMode.Single));
        }
#endif

        internal static bool ShouldLoadGameplayScene(bool isVirtualPlayer, bool isLaunchModeSet, string? gameplayScene, string activeScene) =>
            isVirtualPlayer && isLaunchModeSet && !string.IsNullOrEmpty(gameplayScene) && gameplayScene != activeScene;

        private static void StartBridge(ElympicsMatchLauncher launcher)
        {
            var newBridge = ElympicsHostBridgeFactory.Create(ApplicationParameters.Parameters.HostBridge.GetValue(ElympicsHostBridgeFactory.Auto));
            if (newBridge == null)
                return;
            if (!newBridge.TryStart(launcher))
            {
                newBridge.Dispose();
                return;
            }
            bridge = newBridge;
            launcher.HostEventEmitted += newBridge.DispatchEvent;
            poller = new BridgePoller(newBridge);
            PlayerLoopHelper.AddAction(PlayerLoopTiming.Update, poller);
            ElympicsLogger.LogInfo($"Match launcher host bridge: {newBridge.Name}.");
        }

        internal static void Shutdown()
        {
            Application.quitting -= Shutdown;
            poller?.Stop();
            poller = null;
            if (bridge != null)
            {
                if (ElympicsMatchLauncher.Current != null)
                    ElympicsMatchLauncher.Current.HostEventEmitted -= bridge.DispatchEvent;
                bridge.Dispose();
                bridge = null;
            }
            ElympicsMatchLauncher.Current = null;
        }

        /// <summary>UniTask has no removal API for player loop items: the item ends by returning false.</summary>
        private sealed class BridgePoller : IPlayerLoopItem
        {
            private IElympicsHostBridge? _bridge;

            public BridgePoller(IElympicsHostBridge bridge) => _bridge = bridge;

            public void Stop() => _bridge = null;

            public bool MoveNext()
            {
                var current = _bridge;
                if (current == null)
                    return false;
                try
                {
                    current.Poll();
                }
                catch (System.Exception e)
                {
                    ElympicsLogger.LogException(e);
                }
                return true;
            }
        }
    }
}
