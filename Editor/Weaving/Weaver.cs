#nullable enable

using System;
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

        private const string RequestedVersionsKey = "Elympics.Weaver.RequestedVersions";

#pragma warning disable IDE0060
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
#pragma warning restore IDE0060
        {
            var elympicsPackageRemoved = deletedAssets.Any(path => path.EndsWith($"/{RuntimeWeavingAssemblyName}"));

            var weavingCodePaths = importedAssets.Concat(deletedAssets).Concat(movedAssets)
                .Where(assetPath => CompilationPipeline.GetAssemblyNameFromScriptPath(assetPath)
                    is EditorCodeGenAssemblyName or EditorWeavingAssemblyName or RuntimeWeavingAssemblyName)
                .ToArray();

            if (elympicsPackageRemoved || IsNewWeavingCodeChange(weavingCodePaths))
                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
        }

        /// <remarks>Unity 6.5+ reports unchanged scripts again after the requested recompilation.</remarks>
        private static bool IsNewWeavingCodeChange(string[] paths)
        {
            var requested = SessionState.GetString(RequestedVersionsKey, "").Split('\n').ToHashSet(StringComparer.Ordinal);
            var versions = paths.Select(path => $"{path}@{GetLastWriteTicks(FileUtil.GetPhysicalPath(path))}").ToArray();
            if (versions.All(requested.Contains))
                return false;
            requested.UnionWith(versions);
            SessionState.SetString(RequestedVersionsKey, string.Join("\n", requested));
            return true;
        }

        private static long GetLastWriteTicks(string path) =>
            (Directory.Exists(path) ? Directory.GetLastWriteTimeUtc(path) : File.GetLastWriteTimeUtc(path)).Ticks;
    }
}
