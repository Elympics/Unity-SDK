#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Elympics.Editor.Launcher
{
    /// <summary>Missing scenes do not fail the build on their own: <c>LoadSceneAsync</c> returns null at runtime instead.</summary>
    internal static class ElympicsLauncherBuildValidation
    {
        public static List<string> FindProblems(ElympicsConfig config, IReadOnlyCollection<string> enabledBuildScenes)
        {
            var problems = new List<string>();
            var gameplayScenes = config.AvailableGames
                .Where(game => game != null)
                .Select(game => (Game: game, Scene: game.GameplayScene))
                .ToList();

            var bootScene = config.BootScene;
            if (!string.IsNullOrEmpty(bootScene))
            {
                if (!enabledBuildScenes.Contains(bootScene))
                    problems.Add($"Boot scene {bootScene} is not enabled in Build Settings. "
                        + "Add it, or clear ElympicsConfig > Boot scene to boot straight into the gameplay scene and return to an empty scene after matches.");
                foreach (var (game, _) in gameplayScenes.Where(pair => pair.Scene == bootScene))
                    problems.Add($"Boot scene {bootScene} is also the gameplay scene of {game.GameName}. The boot scene must be a separate scene.");
            }

            foreach (var (game, scene) in gameplayScenes)
                if (string.IsNullOrEmpty(scene))
                    problems.Add($"Game {game.GameName} has no gameplay scene.");
                else if (!enabledBuildScenes.Contains(scene))
                    problems.Add($"Gameplay scene {scene} of {game.GameName} is not enabled in Build Settings.");

            return problems;
        }

        public static List<string> FindWarnings(ElympicsConfig config)
        {
            var warnings = new List<string>();
            if (string.IsNullOrEmpty(config.BootScene))
                warnings.Add($"{nameof(ElympicsConfig)} has no boot scene, so matches return to an empty scene. "
                    + "Set ElympicsConfig > Boot scene to return to your own scene.");
            return warnings;
        }

        public static IReadOnlyCollection<string> EnabledBuildScenes() =>
            EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToHashSet();
    }

    /// <summary>Server builds are skipped: they contain a single gameplay scene and never return to boot.</summary>
    internal sealed class ElympicsLauncherBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (IsServerBuild(report))
                return;
            var config = ElympicsConfig.Load();
            if (config == null)
                return;

            // Build Settings, not the scene list of this particular build: at runtime, LoadSceneAsync resolves paths against the built scene list,
            // and the launcher requirement is stated against Build Settings.
            var scenes = ElympicsLauncherBuildValidation.EnabledBuildScenes();
            foreach (var warning in ElympicsLauncherBuildValidation.FindWarnings(config))
                Debug.LogWarning($"Elympics match launcher: {warning}");
            var problems = ElympicsLauncherBuildValidation.FindProblems(config, scenes);
            if (problems.Count > 0)
                throw new BuildFailedException("Elympics match launcher configuration is invalid:\n- " + string.Join("\n- ", problems));
        }

        private static bool IsServerBuild(BuildReport report) =>
            report.summary.platformGroup == BuildTargetGroup.Standalone
            && (EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server
                || report.summary.GetSubtarget<StandaloneBuildSubtarget>() == StandaloneBuildSubtarget.Server);
    }
}
