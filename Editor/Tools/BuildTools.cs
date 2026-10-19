using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Elympics.Core.Logger;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Elympics
{
    public static class BuildTools
    {
        private const string ServerBuildPath = "serverbuild";
        private const string EngineSubdirectory = "Engine";
        private const string UnityBuildPath = "Unity";

        private const string ServerBuildAppNameLinux = "Unity";
        private const string ServerBuildAppNameWindows = "Unity.exe";

        internal static string EnginePath => Path.Combine(ServerBuildPath, EngineSubdirectory);

        private static readonly Regex MissingModuleRegex = new(
            @"build target (was )?unsupported|LinuxStandalone|scripting backend (\(\w+\) )?(is )?not installed",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private const string MissingModuleErrorMessage =
            "Installation of Unity modules is required: Linux Build Support (Mono) and Linux Dedicated Server Build Support";

        public static void UpdateElympicsGameVersion(string newGameVersion)
        {
            var config = ElympicsConfig.LoadCurrentElympicsGameConfig()
                         ?? throw new ElympicsException("Elympics config not found");

            config.UpdateGameVersion(newGameVersion);
        }

        internal static bool BuildServerWindows(BuildOptions additionalOptions) =>
            BuildServer(ServerBuildAppNameWindows, BuildTarget.StandaloneWindows64, additionalOptions).summary.result == BuildResult.Succeeded;

        internal static BuildReport BuildServerLinux(BuildOptions additionalOptions) => BuildServer(ServerBuildAppNameLinux, BuildTarget.StandaloneLinux64, additionalOptions);

        private static BuildReport BuildServer(string appName, BuildTarget target, BuildOptions additionalOptions)
        {
            ScriptingImplementation? replacedBackend = null;
            // BuildPlayer leaves the Editor's standalone subtarget on Server, which breaks the next player build.
            var previousSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
            try
            {
                var title = $"Building server for {appName}";
                EditorUtility.DisplayProgressBar(title, "Loading elympics game config", 0);
                var config = ElympicsConfig.LoadCurrentElympicsGameConfig()
                             ?? throw new ElympicsException("Elympics config not found");

                config.RefreshGameplayScene();
                var sceneToBuild = new[] { config.GameplayScene };
                EditorUtility.DisplayProgressBar(title, $"Using scene {config.GameplayScene}", 0.15f);

                replacedBackend = SetUpServerScriptingBackend();

                EditorUtility.DisplayProgressBar(title, "Removing old server build path", 0.3f);
                if (Directory.Exists(ServerBuildPath))
                    Directory.Delete(ServerBuildPath, true);

                EditorUtility.DisplayProgressBar(title, "Building player", 0.45f);

                var buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = sceneToBuild,
                    locationPathName = Path.Combine(ServerBuildPath, EngineSubdirectory, UnityBuildPath, appName),
                    targetGroup = BuildTargetGroup.Standalone,
                    target = target,
                    subtarget = (int)StandaloneBuildSubtarget.Server,
                    options = BuildOptions.CompressWithLz4HC | additionalOptions
                };

                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                LogBuildResult(report);

                if (report.summary.result != BuildResult.Succeeded)
                    return report;

                EditorUtility.DisplayProgressBar(title, $"Build finished at {ServerBuildPath}", 1f);

                return report;
            }
            finally
            {
                if (replacedBackend is { } backend)
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Server, backend);
                if (EditorUserBuildSettings.standaloneBuildSubtarget != previousSubtarget)
                    EditorUserBuildSettings.standaloneBuildSubtarget = previousSubtarget;
                EditorUtility.ClearProgressBar();
            }
        }

        /// <returns>The Dedicated Server backend to restore after the build, or null if it wasn't changed.</returns>
        private static ScriptingImplementation? SetUpServerScriptingBackend()
        {
#if UNITY_6000_8_OR_NEWER || UNITY_7000_0_OR_NEWER
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Server);
            ElympicsLogger.LogWarning($"Elympics no longer forces a scripting backend for server builds. Using {backend} from the Dedicated Server player settings.");
            return null;
#else
            var previous = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Server);
            if (previous == ScriptingImplementation.Mono2x)
                return null;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Server, ScriptingImplementation.Mono2x);
            return previous;
#endif
        }

        private static void LogBuildResult(BuildReport report)
        {
            if (report.summary.result == BuildResult.Succeeded)
                ElympicsLogger.LogInfo($"Server build succeeded on {report.summary.outputPath}");
            else
            {
                ElympicsLogger.LogError($"Server build failed with {report.summary.totalErrors} errors");
                ProcessBuildErrors(report);
            }
        }

        private static void ProcessBuildErrors(BuildReport report)
        {
            if (report.summary.totalErrors == 0)
                return;

            if (report.steps.SelectMany(step => step.messages).Any(m => m.type is LogType.Error or LogType.Exception && MissingModuleRegex.IsMatch(m.content)))
                ElympicsLogger.LogError(MissingModuleErrorMessage);
        }
    }
}
