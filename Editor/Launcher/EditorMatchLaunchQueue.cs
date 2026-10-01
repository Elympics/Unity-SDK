#nullable enable

using System;
using System.Linq;
using Elympics.Core.Logger;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elympics.Editor.Launcher
{
    /// <summary>
    /// Launches of the Match Launcher window: enters play mode from the boot scene (or an empty scene) if needed,
    /// then sends the launch command with the window's game ID, data JSON and options JSON.
    /// The pending launch survives the domain reload of entering play mode in <see cref="SessionState"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class EditorMatchLaunchQueue
    {
        private const string PendingKey = "Elympics.MatchLauncher.PendingLaunch";

        [Serializable]
        private struct PendingLaunch
        {
            public ElympicsHostLaunchKind kind;
            public string gameId;
            public string dataJson;
            public string optionsJson;
        }

        static EditorMatchLaunchQueue()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>Launches now in play mode, or enters play mode and launches then. Returns false if the user cancelled saving the open scenes.</summary>
        public static bool Launch(ElympicsHostLaunchKind kind, string gameId, string dataJson, string optionsJson)
        {
            var launch = new PendingLaunch { kind = kind, gameId = gameId, dataJson = dataJson, optionsJson = optionsJson };
            if (EditorApplication.isPlaying)
            {
                LaunchNow(launch);
                return true;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;
            if (ValidBootScene() is { } bootScene)
                _ = EditorSceneManager.OpenScene(bootScene);
            else
                _ = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            SessionState.SetString(PendingKey, JsonUtility.ToJson(launch));
            EditorApplication.EnterPlaymode();
            return true;
        }

        /// <summary>The configured boot scene if the launcher would return to it, else null (the launcher then uses a generated empty scene).</summary>
        private static string? ValidBootScene()
        {
            var config = ElympicsConfig.Load();
            if (config == null)
                return null;
            var bootScene = config.BootScene;
            if (string.IsNullOrEmpty(bootScene))
                return null;
            var inBuild = EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == bootScene);
            var isGameplay = config.AvailableGames.Any(game => game != null && game.GameplayScene == bootScene);
            return inBuild && !isGameplay ? bootScene : null;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
                SessionState.EraseString(PendingKey);
            if (change != PlayModeStateChange.EnteredPlayMode)
                return;
            var pending = SessionState.GetString(PendingKey, "");
            SessionState.EraseString(PendingKey);
            if (string.IsNullOrEmpty(pending))
                return;
            var launch = JsonUtility.FromJson<PendingLaunch>(pending);
            // The launcher is created before the first scene loads, so it exists once play mode is entered.
            EditorApplication.delayCall += () => LaunchNow(launch);
        }

        private static void LaunchNow(PendingLaunch launch)
        {
            var launcher = ElympicsMatchLauncher.Current;
            if (launcher == null)
            {
                ElympicsLogger.LogError("The match launcher does not exist. Is play mode running?");
                return;
            }
            // The launcher reports the result, including a rejection of the JSON, as host events in the window.
            launcher.HandleHostLaunch(launch.kind, launch.gameId ?? "", launch.dataJson ?? "", launch.optionsJson ?? "");
        }
    }
}
