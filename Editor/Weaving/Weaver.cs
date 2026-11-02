#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;

namespace Elympics.Editor.Weaving
{
    /// <summary>
    /// Watches for changes that invalidate previously woven assemblies and requests a clean recompilation.
    /// The actual weaving is performed by ElympicsILPostProcessor (Unity.Elympics.Editor.CodeGen assembly),
    /// which runs automatically as part of Unity's ILPostProcessing pipeline.
    /// </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    internal class AssetPostprocessing : AssetPostprocessor
    {
        private const string EditorCodeGenAssemblyName = "Unity.Elympics.Editor.CodeGen.dll";
        private const string EditorWeavingAssemblyName = "Elympics.Editor.Weaving.dll";

        /// <remark>
        /// All weaved assemblies contain a reference to <see cref="Elympics.Weaving.ProcessedByElympicsAttribute"/>.
        /// This assembly being removed means all such references have to be removed (by requesting script recompilation).
        /// </remark>
        private const string RuntimeWeavingAssemblyName = "Elympics.Weaving.dll";

        private static readonly string[] WeavingAssemblyNames = { EditorCodeGenAssemblyName, EditorWeavingAssemblyName, RuntimeWeavingAssemblyName };

        private const string RequestedVersionsKey = "Elympics.Weaver.RequestedVersions";
        private static readonly long DeletedVersionTicks = DateTime.FromFileTimeUtc(0).Ticks;

#pragma warning disable IDE0060
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
#pragma warning restore IDE0060
        {
            var elympicsPackageRemoved = deletedAssets.Any(path => path.EndsWith($"/{RuntimeWeavingAssemblyName}"));

            var weavingCodePaths = importedAssets.Concat(deletedAssets).Concat(movedAssets)
                .Where(assetPath => WeavingAssemblyNames.Contains(CompilationPipeline.GetAssemblyNameFromScriptPath(assetPath)))
                .ToArray();

            if (elympicsPackageRemoved || IsNewWeavingCodeChange(weavingCodePaths, importedAssets))
                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
        }

        /// <remarks>
        /// Unity 6.5+ reports all scripts of the weaving assemblies again after the requested recompilation,
        /// so all of them are marked as requested, not only the changed ones.
        /// </remarks>
        private static bool IsNewWeavingCodeChange(string[] paths, string[] importedAssets)
        {
            var requested = SessionState.GetString(RequestedVersionsKey, "").Split('\n').ToHashSet(StringComparer.Ordinal);
            // A deleted path always has the same version, so a later deletion of a re-created file must not look already requested.
            _ = requested.RemoveWhere(version => importedAssets.Any(imported => version == $"{imported}@{DeletedVersionTicks}"));
            var versions = paths.Select(GetVersion).ToArray();
            if (versions.All(requested.Contains))
                return false;
            requested.UnionWith(versions);
            requested.UnionWith(GetWeavingSourceFiles().Select(GetVersion));
            SessionState.SetString(RequestedVersionsKey, string.Join("\n", requested));
            return true;
        }

        private static IEnumerable<string> GetWeavingSourceFiles() =>
            CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                .Where(assembly => WeavingAssemblyNames.Contains(Path.GetFileName(assembly.outputPath)))
                .SelectMany(assembly => assembly.sourceFiles);

        private static string GetVersion(string path) => $"{path}@{GetLastWriteTicks(FileUtil.GetPhysicalPath(path))}";

        private static long GetLastWriteTicks(string path) =>
            (Directory.Exists(path) ? Directory.GetLastWriteTimeUtc(path) : File.GetLastWriteTimeUtc(path)).Ticks;
    }
}
