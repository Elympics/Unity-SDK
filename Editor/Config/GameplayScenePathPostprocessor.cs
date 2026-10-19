#nullable enable

using System;
using System.Linq;
using UnityEditor;

namespace Elympics.Editor.Config
{
    /// <summary>Keeps <see cref="ElympicsGameConfig.GameplayScene"/> in step with scene assets that are moved, renamed or reimported.</summary>
    internal class GameplayScenePathPostprocessor : AssetPostprocessor
    {
#pragma warning disable IDE0060
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
#pragma warning restore IDE0060
        {
            if (!importedAssets.Concat(movedAssets).Any(IsScene))
                return;
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(ElympicsGameConfig)}"))
            {
                AssetDatabase.LoadAssetAtPath<ElympicsGameConfig>(AssetDatabase.GUIDToAssetPath(guid))?.RefreshGameplayScene();
            }
        }

        private static bool IsScene(string path) => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
    }
}
