#nullable enable

using System;
using System.IO;
using System.Linq;
using Elympics.Core.Logger;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elympics.Editor.Launcher
{
    /// <summary>The pending launch survives the domain reload of entering play mode in <see cref="SessionState"/>.</summary>
    [InitializeOnLoad]
    internal static class EditorMatchLaunchQueue
    {
        private const string PendingKey = "Elympics.MatchLauncher.PendingLaunch";
        private const string SceneSetupKey = "Elympics.MatchLauncher.SceneSetup";

        [Serializable]
        private struct PendingLaunch
        {
            public ElympicsHostLaunchKind kind;
            public string gameId;
            public string dataJson;
            public string optionsJson;
        }

        [Serializable]
        private struct SavedScene
        {
            public string path;
            public bool isLoaded;
            public bool isActive;
        }

        [Serializable]
        private struct SavedSceneSetup
        {
            public SavedScene[] scenes;
        }

        static EditorMatchLaunchQueue()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

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
            SaveSceneSetup();
            if (GetValidBootScene() is { } bootScene)
                _ = EditorSceneManager.OpenScene(bootScene);
            else
                _ = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            SessionState.SetString(PendingKey, JsonUtility.ToJson(launch));
            EditorApplication.EnterPlaymode();
            return true;
        }

        private static string? GetValidBootScene()
        {
            var config = ElympicsConfig.Load();
            return ElympicsBootScene.FindProblem(config, ElympicsLauncherBuildValidation.GetEnabledBuildScenes().Contains) == null ? config!.BootScene : null;
        }

        private static void SaveSceneSetup()
        {
            var scenes = EditorSceneManager.GetSceneManagerSetup()
                .Where(scene => !string.IsNullOrEmpty(scene.path))
                .Select(scene => new SavedScene { path = scene.path, isLoaded = scene.isLoaded, isActive = scene.isActive })
                .ToArray();
            if (scenes.Length == 0)
            {
                SessionState.EraseString(SceneSetupKey);
                return;
            }
            if (!scenes.Any(scene => scene.isActive))
                scenes[0].isActive = true;
            SessionState.SetString(SceneSetupKey, JsonUtility.ToJson(new SavedSceneSetup { scenes = scenes }));
        }

        private static void RestoreSceneSetup()
        {
            var json = SessionState.GetString(SceneSetupKey, "");
            SessionState.EraseString(SceneSetupKey);
            if (string.IsNullOrEmpty(json))
                return;
            var saved = JsonUtility.FromJson<SavedSceneSetup>(json).scenes;
            if (saved == null || saved.Length == 0)
                return;
            var setup = saved.Where(scene => File.Exists(scene.path))
                .Select(scene => new SceneSetup { path = scene.path, isLoaded = scene.isLoaded, isActive = scene.isActive })
                .ToArray();
            if (setup.Length == 0)
                return;
            if (!setup.Any(scene => scene.isActive))
                setup[0].isActive = true;
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseString(PendingKey);
                RestoreSceneSetup();
            }
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
            // The launcher reports the result, including a rejected JSON, as host events in the window.
            launcher.HandleHostLaunch(launch.kind, launch.gameId ?? "", launch.dataJson ?? "", launch.optionsJson ?? "");
        }
    }
}
