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
        private class BuildPreprocessing : IPreprocessBuildWithReport
        {
            public int callbackOrder => -100;
            public void OnPreprocessBuild(BuildReport report)
            {
                Debug.Log("[Weaver] OnPreprocessBuild");
                WeaveAllAssemblies();
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
                Debug.Log("[Weaver] OnPostprocessAllAssets");
                UpdateAssemblyList();
                WeaveAllAssemblies();
            }
        }

        private static readonly Log Log = new(new LoggableContext());

        private static readonly ComponentController Components = new(new ElympicsRpcComponent());
        private static readonly Stopwatch Timer = new();

        private static readonly List<WeavedAssembly> WeavedAssemblies = new();

        private static void UpdateAssemblyList()
        {
            WeavedAssemblies.Clear();
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(WeaverSettings).FullName}"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<WeaverSettings>(assetPath);
                if (!settings.IsEnabled)
                    continue;
                if (!settings.RequireScriptingSymbols.ValidateSymbols())
                    continue;
                foreach (var weavedAssembly in settings.WeavedAssemblies) WeavedAssemblies.Add(weavedAssembly);
            }
            Debug.Log($"[Weaver]: m_WeavedAssemblies: {string.Join(", ", WeavedAssemblies)}");
        }

        [UsedImplicitly]
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Debug.Log("[Weaver] InitializeOnLoadMethod");

            Log.Info("Weaver Settings", "Subscribing to next assembly reload.", false);
            Debug.Log("[Weaver] Subscribing to assemblyCompilationFinished");
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            Debug.Log("[Weaver] Subscribing to compilationFinished");
            CompilationPipeline.compilationFinished += OnCompilationFinished;
#if UNITY_2022_2_OR_NEWER
            Debug.Log("[Weaver] Subscribing to assemblyCompilationNotRequired");
            CompilationPipeline.assemblyCompilationNotRequired += OnAssemblyCompilationNotRequired;
#endif

            AssemblyUtility.PopulateAssemblyCache();
            UpdateAssemblyList();
            WeaveAllAssemblies();
        }

        [PostProcessScene]
        public static void PostprocessScene()
        {
            Debug.Log("[Weaver] PostProcessScene");

            UpdateAssemblyList();

            if (!BuildPipeline.isBuildingPlayer)
                return;
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.buildIndex != 0)
                return;

            WeaveAllAssemblies();
        }

        /// <summary>
        /// Invoked whenever one of our assemblies has compelted compliling.
        /// </summary>
        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] compilerMessages)
        {
            Debug.Log($"[Weaver] Compiled assembly: {assemblyPath}, {compilerMessages.Length} messages");
            foreach (var compilerMessage in compilerMessages)
                switch (compilerMessage.type)
                {
                    case CompilerMessageType.Error:
                        Debug.LogError($"[{compilerMessage.file}:{compilerMessage.line}:{compilerMessage.column}] {compilerMessage.message}");
                        break;
                    case CompilerMessageType.Warning:
                        Debug.LogWarning($"[{compilerMessage.file}:{compilerMessage.line}:{compilerMessage.column}] {compilerMessage.message}");
                        break;
                    case CompilerMessageType.Info:
                    default:
                        Debug.Log($"[{compilerMessage.file}:{compilerMessage.line}:{compilerMessage.column}] {compilerMessage.message}");
                        break;
                }

            WeaveAssembly(assemblyPath);
        }

        private static void OnCompilationFinished(object context)
        {
            Debug.Log("[Weaver] Compiled all assemblies");
            WeaveAllAssemblies();
        }

#if UNITY_2022_2_OR_NEWER
        private static void OnAssemblyCompilationNotRequired(string assemblyPath)
        {
            Debug.Log($"[Weaver] Assembly compilation not required: {assemblyPath}");
            WeaveAssembly(assemblyPath);
        }
#endif

        private static void WeaveAllAssemblies()
        {
            foreach (var assembly in WeavedAssemblies)
                WeaveAssembly(assembly.RelativePath);
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
        private static void WeaveAssembly(string assemblyPath)
        {
            var runId = counter++;
            Debug.Log($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly called");
            using var lockScope = new LockReloadAssembliesScope();
            if (string.IsNullOrEmpty(assemblyPath))
                return;
            var weavedAssembly = WeavedAssemblies
                .FirstOrDefault(a => Path.GetFileName(a.GetSystemPath()) == Path.GetFileName(assemblyPath));
            if (weavedAssembly == null)
            {
                Debug.Log($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: Not in the list");
                return;
            }

            if (!File.Exists(assemblyPath))
            {
                if (weavedAssembly.ShouldThrowIfNotFound)
                    ElympicsLogger.LogWarning($"Could not find assembly file: {assemblyPath}");
                Debug.Log($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: Exists = false");
                return;
            }
            Debug.Log($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: Exists = true");
            if (HasBeenAlreadyWeaved(assemblyPath))
            {
                Debug.Log($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: HasBeenAlreadyWeaved = true");
                return;
            }
            Debug.Log($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: HasBeenAlreadyWeaved = false");


            var name = Path.GetFileNameWithoutExtension(assemblyPath);

            Log.Info(name, "Starting", false);

            var filePath = Path.Combine(Constants.ProjectRoot, assemblyPath);

            if (!File.Exists(filePath))
            {
                Log.Error(name, "Unable to find assembly at path '" + filePath + "'.", true);
                Debug.Log($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: file Exists = false");
                return;
            }
            Debug.Log($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly: file Exists = true");

            using (var assemblyStream = new FileStream(assemblyPath, FileMode.Open, FileAccess.ReadWrite))
            {
                using var moduleDefinition = ModuleDefinition.ReadModule(assemblyStream, GetReaderParameters(assemblyPath));
                Components.VisitModule(moduleDefinition, Log);

                // Save
                moduleDefinition.Write(GetWriterParameters());
            }

            Log.Info("Weaver Settings", "Weaving Successfully Completed", false);

            // Stats
            Log.Info(name, "Time ms: " + Timer.ElapsedMilliseconds, false);
            Log.Info(name, "Types: " + Components.totalTypesVisited, false);
            Log.Info(name, "Methods: " + Components.totalMethodsVisited, false);
            Log.Info(name, "Fields: " + Components.totalFieldsVisited, false);
            Log.Info(name, "Properties: " + Components.totalPropertiesVisited, false);
            Log.Info(name, "Complete", false);

            Debug.Log($"[Weaver]:{runId} [{assemblyPath}] WeaveAssembly completed");
        }

        private class LockReloadAssembliesScope : IDisposable
        {
            public LockReloadAssembliesScope() => EditorApplication.LockReloadAssemblies();
            public void Dispose() => EditorApplication.UnlockReloadAssemblies();
        }
    }
}

