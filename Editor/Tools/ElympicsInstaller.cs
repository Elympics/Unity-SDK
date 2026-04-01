using System.IO;
using System.Linq;
using UnityEditor;

namespace Elympics
{
    public class ElympicsInstaller : AssetPostprocessor
    {
#pragma warning disable IDE0060
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
#pragma warning restore IDE0060
        {
            const string assemblyName = "Elympics.Analyzers";
            var targetPath = $"Assets/{assemblyName}.dll";
            var sourcePath = importedAssets.Concat(deletedAssets).FirstOrDefault(path => path.EndsWith($"Editor/{assemblyName}.dll"));
            if (sourcePath is null)
                return;

            if (!importedAssets.Contains(sourcePath))
            {
                ElympicsLogger.LogDebug($"[Installer] Removing {targetPath}");
                File.Delete(targetPath);
                File.Delete(targetPath + ".meta");
                return;
            }
            ElympicsLogger.LogDebug($"[Installer] {(File.Exists(targetPath) ? "Updating" : "Creating")} {targetPath} from {sourcePath}");
            File.Copy(sourcePath, targetPath, true);
            File.Copy(sourcePath + ".meta", targetPath + ".meta", true);
        }
    }
}
