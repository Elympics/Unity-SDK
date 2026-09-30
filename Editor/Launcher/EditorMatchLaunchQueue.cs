#nullable enable

using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Elympics.Core.Logger;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Elympics.Editor.Launcher
{
    /// <summary>
    /// One-click launches of the Match Launcher window: enters play mode from the boot scene (or an empty scene) if needed,
    /// then launches the chosen mode of the selected game with its <see cref="ElympicsGameConfig"/> defaults.
    /// The pending launch survives the domain reload of entering play mode in <see cref="SessionState"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class EditorMatchLaunchQueue
    {
        private const string PendingKey = "Elympics.MatchLauncher.PendingLaunch";

        static EditorMatchLaunchQueue()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>Launches now in play mode, or enters play mode and launches then.</summary>
        public static void Launch(ElympicsHostLaunchKind kind, string gameId)
        {
            if (EditorApplication.isPlaying)
            {
                LaunchNow(kind, gameId);
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (ValidBootScene() is { } bootScene)
                _ = EditorSceneManager.OpenScene(bootScene);
            else
                _ = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            SessionState.SetString(PendingKey, $"{kind}|{gameId}");
            EditorApplication.EnterPlaymode();
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
            var parts = pending.Split('|');
            if (parts.Length != 2 || !Enum.TryParse<ElympicsHostLaunchKind>(parts[0], out var kind))
                return;
            // The launcher is created before the first scene loads, so it exists once play mode is entered.
            EditorApplication.delayCall += () => LaunchNow(kind, parts[1]);
        }

        private static void LaunchNow(ElympicsHostLaunchKind kind, string gameId)
        {
            var launcher = ElympicsMatchLauncher.Current;
            if (launcher == null)
            {
                ElympicsLogger.LogError("The match launcher does not exist. Is play mode running?");
                return;
            }
            var game = ElympicsConfig.Load()?.AvailableGames.FirstOrDefault(config => config != null && config.GameId == gameId);
            var launch = kind switch
            {
                ElympicsHostLaunchKind.SinglePlayer => launcher.LaunchSinglePlayerAsync(gameId),
                ElympicsHostLaunchKind.HalfRemoteServer => launcher.LaunchHalfRemoteServerAsync(gameId),
                ElympicsHostLaunchKind.HalfRemoteClient => launcher.LaunchHalfRemoteClientAsync(gameId),
                ElympicsHostLaunchKind.SnapshotReplay => launcher.LaunchSnapshotReplayAsync(gameId, new SnapshotReplayLaunchData(game != null ? game.SnapshotFilePath : "")),
                ElympicsHostLaunchKind.Online => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Online launches need match data. Use the JSON launch."),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
            LogResult(launch).Forget();
        }

        private static async UniTaskVoid LogResult(UniTask<ElympicsLaunchResult> launch)
        {
            var result = await launch;
            if (!result.Success)
                ElympicsLogger.LogWarning($"Match Launcher: {result}");
        }
    }
}
