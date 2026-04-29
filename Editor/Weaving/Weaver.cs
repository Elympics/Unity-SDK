using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Elympics.Editor.Weaving.Settings;
using UnityEditor;
using UnityEditor.Compilation;

#nullable enable

namespace Elympics.Editor.Weaving
{
    // Watches for changes that invalidate previously woven assemblies and requests a clean recompilation.
    // The actual weaving is performed by ElympicsILPostProcessor (Elympics.Editor.CodeGen assembly),
    // which runs automatically as part of Unity's ILPostProcessing pipeline.
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    internal class AssetPostprocessing : AssetPostprocessor
    {
        private const string EditorCodeGenAssemblyName = "Elympics.Editor.CodeGen.dll";
        private const string EditorWeavingAssemblyName = "Elympics.Editor.Weaving.dll";
        private const string RuntimeWeavingAssemblyName = "Elympics.Weaving.dll";

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (deletedAssets.Any(path => path.EndsWith($"/{RuntimeWeavingAssemblyName}")))
            {
                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
                return;
            }

            var concatenatedAssets = importedAssets.Concat(deletedAssets).Concat(movedAssets);

            var elympicsWeavingCodeUpdated = concatenatedAssets
                .Any(assetPath => CompilationPipeline.GetAssemblyNameFromScriptPath(assetPath)
                    is EditorCodeGenAssemblyName or EditorWeavingAssemblyName or RuntimeWeavingAssemblyName);

            var weaverSettingsChanged = concatenatedAssets
                .Any(assetPath => AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(WeaverSettings));

            if (elympicsWeavingCodeUpdated || weaverSettingsChanged)
                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
        }
    }
}
