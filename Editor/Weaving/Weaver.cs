using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Elympics.Editor.Weaving.Components;
using Elympics.Editor.Weaving.Components.Elympics;
using Elympics.Editor.Weaving.Extensions;
using Elympics.Editor.Weaving.Settings;
using Elympics.Weaving;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine.SceneManagement;

#nullable enable

namespace Elympics.Editor.Weaving
{
    [InitializeOnLoad]
    internal static class Weaver
    {
        private const string EditorWeavingAssemblyName = "Elympics.Editor.Weaving.dll";
        private const string RuntimeWeavingAssemblyName = "Elympics.Weaving.dll";

        private static readonly ComponentController Components = new(new ElympicsRpcComponent());
        private static readonly Stopwatch Timer = new();

        private static readonly HashSet<string> WeavedAssemblyNames = new();

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

        static Weaver()
        {
            ElympicsLogger.LogDebug("[Weaver] InitializeOnLoad");

            ElympicsLogger.LogDebug("[Weaver] Subscribing to compilationFinished");
            CompilationPipeline.compilationFinished += OnCompilationFinished;

            UpdateWeavedAssembliesList();
            WeaveAssemblies(CompilationPipeline.GetAssemblies());
        }

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
                    if (!WeavedAssemblyNames.Add(Path.GetFileName(weavedAssembly.Name)))
                        ElympicsLogger.LogWarning($"[Weaver] Assembly {Path.GetFileName(weavedAssembly.Name)} already in list");
            }
            ElympicsLogger.LogDebug($"[Weaver] Updated WeavedAssemblyNames: [{string.Join(", ", WeavedAssemblyNames)}]");
        }

        private static bool HasBeenAlreadyWeaved(string assemblyPath)
        {
            using var moduleDefinition = ModuleDefinition.ReadModule(assemblyPath, GetReaderParameters(assemblyPath));
            var soughtAttributeType = moduleDefinition.ImportReference(typeof(ProcessedByElympicsAttribute));
            return moduleDefinition.Assembly.CustomAttributes
                .Any(attribute => attribute.AttributeType.FullName == soughtAttributeType.FullName);
        }

        private static readonly object Lock = new();
        private static int counter;
        private static void WeaveAssemblies(IList<Assembly> assemblies)
        {
            using var lockScope = new LockReloadAssembliesScope();

            var processedAssemblies = assemblies.Where(assembly => WeavedAssemblyNames.Contains(Path.GetFileName(assembly.outputPath))).ToArray();
            // ReSharper disable once RedundantAssignment
            var skippedAssemblies = assemblies.Where(assembly => !WeavedAssemblyNames.Contains(Path.GetFileName(assembly.outputPath))).ToArray();
            ElympicsLogger.LogDebug("[Weaver] Processing assemblies...\n"
                + $"To process ({processedAssemblies.Length}): [{string.Join(", ", processedAssemblies.Select(assembly => assembly.outputPath))}]\n"
                + $"To skip ({skippedAssemblies.Length}): [{string.Join(", ", skippedAssemblies.Select(assembly => assembly.outputPath))}]");

            lock (Lock)
                foreach (var assembly in processedAssemblies)
                    WeaveAssembly(assembly.outputPath);

            static void WeaveAssembly(string assemblyPath)
            {
                // ReSharper disable once RedundantAssignment
                var runId = counter++;
                ElympicsLogger.LogDebug($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly called");
                Timer.Restart();

                if (!File.Exists(assemblyPath))
                {
                    ElympicsLogger.LogDebug($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: Exists = false");
                    return;
                }
                ElympicsLogger.LogDebug($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: Exists = true");
                if (HasBeenAlreadyWeaved(assemblyPath))
                {
                    ElympicsLogger.LogDebug($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: HasBeenAlreadyWeaved = true");
                    return;
                }
                ElympicsLogger.LogDebug($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: HasBeenAlreadyWeaved = false");

                using (var moduleDefinition = ModuleDefinition.ReadModule(assemblyPath, GetReaderParameters(assemblyPath)))
                {
                    Components.VisitModule(moduleDefinition);
                    moduleDefinition.Write(assemblyPath, GetWriterParameters());
                }

                Timer.Stop();
                ElympicsLogger.LogDebug($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly completed\n"
                    + $"Time: {Timer.ElapsedMilliseconds} ms\n"
                    + $"Types visited: {Components.TotalTypesVisited}\n"
                    + $"Methods visited: {Components.TotalMethodsVisited}\n"
                    + $"Fields visited: {Components.TotalFieldsVisited}\n"
                    + $"Properties visited: {Components.TotalPropertiesVisited}");
            }
        }

        #region Callbacks

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

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private class AssetPostprocessing : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                ElympicsLogger.LogDebug($"[Weaver] OnPostprocessAllAssets ({importedAssets.Length} imported, {deletedAssets.Length} deleted, {movedFromAssetPaths.Length} moved)");

                var concatenatedAssets = importedAssets.Concat(deletedAssets).Concat(movedAssets);
                var elympicsWeavingCodeUpdated = concatenatedAssets
                    .Any(assetPath => CompilationPipeline.GetAssemblyNameFromScriptPath(assetPath) is EditorWeavingAssemblyName or RuntimeWeavingAssemblyName);
                var weaverSettingsChanged = concatenatedAssets
                    .Any(assetPath => AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(WeaverSettings));
                ElympicsLogger.LogDebug($"[Weaver] Elympics.Weaving code updated: {elympicsWeavingCodeUpdated}, Weaver settings changed: {weaverSettingsChanged}");

                if (weaverSettingsChanged)
                    UpdateWeavedAssembliesList();
                if (elympicsWeavingCodeUpdated)
                    CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
                else if (weaverSettingsChanged)
                    WeaveAssemblies(CompilationPipeline.GetAssemblies());
            }
        }

        #endregion

        private class LockReloadAssembliesScope : IDisposable
        {
            public LockReloadAssembliesScope() => EditorApplication.LockReloadAssemblies();
            public void Dispose() => EditorApplication.UnlockReloadAssemblies();
        }
    }
}

