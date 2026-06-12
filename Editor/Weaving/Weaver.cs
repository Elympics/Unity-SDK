#nullable enable

using System.Diagnostics.CodeAnalysis;
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

#pragma warning disable IDE0060
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
#pragma warning restore IDE0060
        {
            var elympicsPackageRemoved = deletedAssets.Any(path => path.EndsWith($"/{RuntimeWeavingAssemblyName}"));

            var elympicsWeavingCodeUpdated = importedAssets.Concat(deletedAssets).Concat(movedAssets)
                .Any(assetPath => CompilationPipeline.GetAssemblyNameFromScriptPath(assetPath)
                    is EditorCodeGenAssemblyName or EditorWeavingAssemblyName or RuntimeWeavingAssemblyName);

            if (elympicsPackageRemoved || elympicsWeavingCodeUpdated)
                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
        }
    }
}
