#nullable enable

using System;
using System.Linq;
using UnityEditor;

namespace Elympics.Editor.Config
{
    internal static class GameConfigAssets
    {
        private const string ElympicsPackagesPrefix = "Packages/com.daftmobile.";

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
