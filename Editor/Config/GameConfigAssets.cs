#nullable enable

using System;
using System.Linq;
using UnityEditor;

namespace Elympics.Editor.Config
{
    internal static class GameConfigAssets
    {
        private const string ElympicsPackagesPrefix = "Packages/com.daftmobile.";

        /// <summary>Finds the project's <see cref="ElympicsGameConfig"/> assets, without the test fixtures of Elympics packages.</summary>
        public static string[] FindGuids() => AssetDatabase.FindAssets($"t:{nameof(ElympicsGameConfig)}")
            .Where(guid => !IsPackageTestAsset(AssetDatabase.GUIDToAssetPath(guid)))
            .ToArray();

        private static bool IsPackageTestAsset(string path)
        {
            if (!path.StartsWith(ElympicsPackagesPrefix, StringComparison.Ordinal))
                return false;
            var packageRootEnd = path.IndexOf('/', ElympicsPackagesPrefix.Length);
            return packageRootEnd >= 0 && path.AsSpan(packageRootEnd).StartsWith("/Tests/", StringComparison.Ordinal);
        }
    }
}
