using System.IO;
using UnityEditor;

namespace Elympics
{
    public class ElympicsInstaller : AssetPostprocessor
    {
#pragma warning disable IDE0060
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
#pragma warning restore IDE0060
        {
            InstallMissingUnityWebRtcMetaFiles();
            AssetDatabase.Refresh();
        }

        private static void InstallMissingUnityWebRtcMetaFiles()
        {
            // source: https://github.com/Unity-Technologies/com.unity.webrtc/tree/3.0.0-pre.6/Runtime/Plugins/iOS/webrtc.framework
            const string infoPlistPath = "Packages/com.unity.webrtc/Runtime/Plugins/iOS/webrtc.framework/Info.plist";
            const string webrtcPath = "Packages/com.unity.webrtc/Runtime/Plugins/iOS/webrtc.framework/webrtc";
            var isPackageInstalled = File.Exists(infoPlistPath) && File.Exists(webrtcPath);
            var areMetaFilesInstalled = File.Exists(infoPlistPath + ".meta") && File.Exists(webrtcPath + ".meta");
            if (!isPackageInstalled || areMetaFilesInstalled)
                return;
            File.WriteAllText(infoPlistPath + ".meta", "fileFormatVersion: 2\nguid: 16178afdbd49d46cda17f7aed169cf63\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: ");
            File.WriteAllText(webrtcPath + ".meta", "fileFormatVersion: 2\nguid: 7549ec121d0b84c61a80170d5e6466a2\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: ");
        }
    }
}
