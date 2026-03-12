using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Elympics.Editor;
using Elympics.Weaving;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

#nullable enable

namespace Elympics.Weaver
{
    public static class Weaver
    {
        private static readonly Log Log = new(new LoggableContext());

        private static readonly ComponentController Components = new(new ElympicsRpcComponent());
        private static readonly Stopwatch Timer = new();

        private static readonly HashSet<string> WeavedAssemblyNames = new();

        private static void UpdateWeavedAssembliesList()
        {
            WeavedAssemblyNames.Clear();
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(WeaverSettings).FullName}"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<WeaverSettings>(assetPath);
                if (!settings.IsEnabled)
                    continue;
                if (!settings.RequireScriptingSymbols.ValidateSymbols())
                    continue;
                foreach (var weavedAssembly in settings.WeavedAssemblies)
                    WeavedAssemblyNames.Add(Path.GetFileName(weavedAssembly.Name));
            }
            ElympicsLogger.LogDebug($"[Weaver] Updated WeavedAssemblyNames: [{string.Join(", ", WeavedAssemblyNames)}]");
        }

        [UsedImplicitly]
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            ElympicsLogger.LogDebug("[Weaver] InitializeOnLoadMethod");

            Debug.Log("[Weaver] Subscribing to compilationFinished");
            CompilationPipeline.compilationFinished += OnCompilationFinished;

            UpdateWeavedAssembliesList();
            WeaveAssemblies(CompilationPipeline.GetAssemblies());
        }

        [PostProcessScene]
        public static void PostprocessScene()
        {
            ElympicsLogger.LogDebug("[Weaver] PostProcessScene");

            UpdateWeavedAssembliesList();

            if (!BuildPipeline.isBuildingPlayer)
                return;
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.buildIndex != 0)
                return;

            WeaveAssemblies(CompilationPipeline.GetAssemblies());
        }

        private static void OnCompilationFinished(object context)
        {
            ElympicsLogger.LogDebug("[Weaver] OnCompilationFinished");
            WeaveAssemblies(CompilationPipeline.GetAssemblies());
        }

        private static ReaderParameters GetReaderParameters(string assemblyPath) =>
            new()
            {
                ReadingMode = ReadingMode.Immediate,
                InMemory = true,
                AssemblyResolver = new WeaverAssemblyResolver(assemblyPath),
                ReadSymbols = true,
                SymbolReaderProvider = new PdbReaderProvider(),
            };

        private static WriterParameters GetWriterParameters() =>
            new()
            {
                WriteSymbols = true,
                SymbolWriterProvider = new PdbWriterProvider(),
            };

        private static bool HasBeenAlreadyWeaved(string assemblyPath)
        {
            using var assemblyStream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read);
            using var moduleDefinition = ModuleDefinition.ReadModule(assemblyStream,
                GetReaderParameters(assemblyPath));
            var soughtAttributeType = moduleDefinition.ImportReference(typeof(ProcessedByElympicsAttribute));
            return moduleDefinition.Assembly.CustomAttributes
                .Any(attribute => attribute.AttributeType.FullName == soughtAttributeType.FullName);
        }

        private static int counter;
        private static void WeaveAssemblies(IList<Assembly> assemblies)
        {
            using var lockScope = new LockReloadAssembliesScope();

            var processedAssemblies = assemblies.Where(assembly => WeavedAssemblyNames.Contains(Path.GetFileName(assembly.outputPath))).ToArray();
            var skippedAssemblies = assemblies.Where(assembly => !WeavedAssemblyNames.Contains(Path.GetFileName(assembly.outputPath))).ToArray();
            ElympicsLogger.LogDebug("[Weaver] Processing assemblies...\n"
                + $"To process ({processedAssemblies.Length}): [{string.Join(", ", processedAssemblies.Select(assembly => assembly.outputPath))}]\n"
                + $"To skip ({skippedAssemblies.Length}): [{string.Join(", ", skippedAssemblies.Select(assembly => assembly.outputPath))}]");

            foreach (var assembly in processedAssemblies)
                WeaveAssembly(assembly.outputPath);

            static void WeaveAssembly(string assemblyPath)
            {
                var runId = counter++;
                ElympicsLogger.LogDebug($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly called");
                Timer.Restart();

                if (HasBeenAlreadyWeaved(assemblyPath))
                {
                    ElympicsLogger.LogDebug($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: HasBeenAlreadyWeaved = true");
                    return;
                }
                ElympicsLogger.LogDebug($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: HasBeenAlreadyWeaved = false");

                using (var assemblyStream = new FileStream(assemblyPath, FileMode.Open, FileAccess.ReadWrite))
                {
                    using var moduleDefinition = ModuleDefinition.ReadModule(assemblyStream, GetReaderParameters(assemblyPath));
                    Components.VisitModule(moduleDefinition, Log);
                    moduleDefinition.Write(GetWriterParameters());
                }

                Timer.Stop();
                ElympicsLogger.LogDebug($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly completed\n"
                    + $"Time: {Timer.ElapsedMilliseconds} ms\n"
                    + $"Types visited: {Components.totalTypesVisited}\n"
                    + $"Methods visited: {Components.totalMethodsVisited}\n"
                    + $"Fields visited: {Components.totalFieldsVisited}\n"
                    + $"Properties visited: {Components.totalPropertiesVisited}");
            }
        }

        private class LockReloadAssembliesScope : IDisposable
        {
            public LockReloadAssembliesScope() => EditorApplication.LockReloadAssemblies();
            public void Dispose() => EditorApplication.UnlockReloadAssemblies();
        }

        private class BuildPreprocessing : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        {
            public int callbackOrder => -100;

            public void OnPreprocessBuild(BuildReport report)
            {
                ElympicsLogger.LogDebug("[Weaver] OnPreprocessBuild");
                WeaveAssemblies(CompilationPipeline.GetAssemblies(AssembliesType.Player));
            }

            public void OnPostprocessBuild(BuildReport report)
            {
                ElympicsLogger.LogDebug("[Weaver] OnPostprocessBuild");
                WeaveAssemblies(CompilationPipeline.GetAssemblies(AssembliesType.Player));
            }
        }

        private class LoggableContext : ILogable
        {
            string ILogable.label => nameof(Weaver);
        }

        private class AssetPostprocessing : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                ElympicsLogger.LogDebug($"[Weaver] OnPostprocessAllAssets ({importedAssets.Length} imported, {deletedAssets.Length} deleted, {movedFromAssetPaths.Length} moved)");
                var weaverSettingsChanged = importedAssets.Concat(deletedAssets).Concat(movedAssets)
                    .Any(assetPath => AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(WeaverSettings));
                if (!weaverSettingsChanged)
                    return;
                UpdateWeavedAssembliesList();
                WeaveAssemblies(CompilationPipeline.GetAssemblies());
            }
        }
    }
}

