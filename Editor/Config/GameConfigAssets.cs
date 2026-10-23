#nullable enable

using System;
using System.Linq;
using UnityEditor;

namespace Elympics.Editor.Config
{
    internal static class GameConfigAssets
    {
        private const string PackageTestsPath = "Packages/com.daftmobile.elympics/Tests/";

        /// <summary>Finds the project's <see cref="ElympicsGameConfig"/> assets, without the SDK's test fixtures.</summary>
        public static string[] FindGuids() => AssetDatabase.FindAssets($"t:{nameof(ElympicsGameConfig)}")
            .Where(guid => !AssetDatabase.GUIDToAssetPath(guid).StartsWith(PackageTestsPath, StringComparison.Ordinal))
            .ToArray();
    }
}
