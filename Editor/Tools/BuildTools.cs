using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Elympics.Core.Logger;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Elympics
{
    public static class BuildTools
    {
        private const string ServerBuildPath = "serverbuild";
        private const string EngineSubdirectory = "Engine";
        private const string BotSubdirectory = "Bot";
        private const string UnityBuildPath = "Unity";

        private const string ServerBuildAppNameLinux = "Unity";
        private const string ServerBuildAppNameWindows = "Unity.exe";

        internal static string EnginePath => Path.Combine(ServerBuildPath, EngineSubdirectory);
        internal static string BotPath => Path.Combine(ServerBuildPath, BotSubdirectory);

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

        internal static bool BuildServerWindows(BuildOptions additionalOptions) => BuildServer(ServerBuildAppNameWindows, BuildTarget.StandaloneWindows64, additionalOptions);

        internal static BuildReport BuildServerLinux(BuildOptions additionalOptions) => BuildServer(ServerBuildAppNameLinux, BuildTarget.StandaloneLinux64, additionalOptions);

        private static bool? IsLinuxModuleInstalled()
        {
            var moduleManager = Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor.dll");
            var isPlatformSupportLoadedByBuildTarget = moduleManager?.GetMethod("IsPlatformSupportLoadedByBuildTarget", BindingFlags.Static | BindingFlags.NonPublic);
            return (bool?)isPlatformSupportLoadedByBuildTarget?.Invoke(null, new object[] { BuildTarget.StandaloneLinux64 });
        }

        internal static BuildReport BuildElympicsServerLinux(BuildOptions additionalOptions)
        {
            // if (IsLinuxModuleInstalled() is false)
            // {
            //     ElympicsLogger.LogError(MissingModuleErrorMessage);
            //     return false;
            // }

            return BuildServerLinux(additionalOptions);
        }

        private static BuildReport BuildServer(string appName, BuildTarget target, BuildOptions additionalOptions)
        {
            try
            {
                var title = $"Building server for {appName}";
                EditorUtility.DisplayProgressBar(title, "Loading elympics game config", 0);
                var config = ElympicsConfig.LoadCurrentElympicsGameConfig()
                             ?? throw new ElympicsException("Elympics config not found");

                var sceneToBuild = new[] { config.GameplayScene };
                EditorUtility.DisplayProgressBar(title, $"Using scene {config.GameplayScene}", 0.15f);

                const BuildTargetGroup buildTargetGroup = BuildTargetGroup.Standalone;
                var oldScriptingBackend = PlayerSettings.GetScriptingBackend(buildTargetGroup);
                PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.Mono2x);

                EditorUtility.DisplayProgressBar(title, "Removing old server build path", 0.3f);
                if (Directory.Exists(ServerBuildPath))
                    Directory.Delete(ServerBuildPath, true);

                EditorUtility.DisplayProgressBar(title, "Building player", 0.45f);

                var buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = sceneToBuild,
                    locationPathName = Path.Combine(ServerBuildPath, EngineSubdirectory, UnityBuildPath, appName),
                    targetGroup = buildTargetGroup,
                    target = target,
                    subtarget = (int)StandaloneBuildSubtarget.Server,
                    options = BuildOptions.CompressWithLz4HC | additionalOptions
                };

                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                LogBuildResult(report);

                // Restore
                PlayerSettings.SetScriptingBackend(buildTargetGroup, oldScriptingBackend);

                if (report.summary.result != BuildResult.Succeeded)
                    return report;

                EditorUtility.DisplayProgressBar(title, $"Build finished at {ServerBuildPath}", 1f);

                return report;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
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
