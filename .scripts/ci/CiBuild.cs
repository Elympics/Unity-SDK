// CI only: build_sample.sh copies this into the testing project's Assets/Editor and calls CiBuild.Build.
// It isn't part of the package.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CiBuild
{
    /// <summary>
    /// Builds the scenes in CI_BUILD_SCENES (';'-separated, relative to the project) for the active build target into CI_BUILD_OUTPUT,
    /// with settings that minimize build time. WebGL uses IL2CPP; the other targets use Mono.
    /// </summary>
    public static void Build()
    {
        var target = EditorUserBuildSettings.activeBuildTarget;
        var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
        var scenes = (Environment.GetEnvironmentVariable("CI_BUILD_SCENES") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
        var output = Environment.GetEnvironmentVariable("CI_BUILD_OUTPUT") ?? "Build";
        if (scenes.Length == 0 || scenes.Any(scene => !File.Exists(scene)))
            Fail($"CI_BUILD_SCENES must list existing scenes, got: [{string.Join(", ", scenes)}]");

        PlayerSettings.SetManagedStrippingLevel(namedTarget, ManagedStrippingLevel.Low);
        PlayerSettings.stripEngineCode = false;
        if (target == BuildTarget.WebGL)
        {
            PlayerSettings.SetScriptingBackend(namedTarget, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(namedTarget, Il2CppCompilerConfiguration.Debug);
            PlayerSettings.SetIl2CppCodeGeneration(namedTarget, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.webAssemblyTable = false;
            output = Path.Combine(output, "WebGL");
        }
        else
        {
            PlayerSettings.SetScriptingBackend(namedTarget, ScriptingImplementation.Mono2x);
            output = Path.Combine(output, target.ToString(), "Sample");
        }

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = target,
            options = BuildOptions.None,
        });
        Debug.Log($"CiBuild: {report.summary.result} in {report.summary.totalTime}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors");
        if (report.summary.result != BuildResult.Succeeded)
            Fail($"Build {report.summary.result}");
        EditorApplication.Exit(0);
    }

    private static void Fail(string message)
    {
        Debug.LogError("CiBuild: " + message);
        EditorApplication.Exit(1);
    }
}
