using System.IO;
using Elympics.Core.Logger;
using Elympics.Editor.Config;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics.Editor
{
    public static class ElympicsTools
    {
        [MenuItem(ElympicsEditorMenuPaths.MANAGE_GAMES_IN_ELYMPICS, priority = 1)]
        internal static void OpenManageGamesInElympicsWindow()
        {
            var elympicsConfig = LoadOrCreateConfig();
            var serializedElympicsConfig = new SerializedObject(elympicsConfig);
            _ = ManageGamesInElympicsWindow.ShowWindow(serializedElympicsConfig);
        }

        private static ElympicsConfig LoadOrCreateConfig() => ElympicsConfig.Load() ?? CreateNewConfig();

        [MenuItem(ElympicsEditorMenuPaths.SETUP_MENU_PATH, priority = 2)]
        public static void SelectOrCreateConfig()
        {
            var config = LoadOrCreateConfig();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;
        }

        [MenuItem(ElympicsEditorMenuPaths.RESET_IDS_MENU_PATH, priority = 3)]
        public static void ResetIds() => SceneNetworkIdAssigner.ResetAllIds(SceneManager.GetActiveScene());

        internal static void EnsureElympicsResourcesDirectoryExists()
        {
            if (Directory.Exists(ElympicsConfig.ElympicsResourcesPath))
                return;

            ElympicsLogger.LogInfo("Creating Elympics resources directory...");
            _ = Directory.CreateDirectory(ElympicsConfig.ElympicsResourcesPath);
            ElympicsLogger.LogInfo("Elympics resources directory created successfully.");
        }

        private static ElympicsConfig CreateNewConfig()
        {
            EnsureElympicsResourcesDirectoryExists();

            var newConfig = ScriptableObject.CreateInstance<ElympicsConfig>();

            const string resourcesDirectory = "Assets/Resources/";
            // TODO: there is probably some hack possible to get path to current Elympics directory
            var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(resourcesDirectory + ElympicsConfig.PathInResources + ".asset");
            AssetDatabase.CreateAsset(newConfig, assetPathAndName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return newConfig;
        }

        [MenuItem(ElympicsEditorMenuPaths.BUILD_WINDOWS_SERVER, priority = 4)]
        private static void BuildServerWindows() => BuildTools.BuildServerWindows(BuildOptions.None);

        [MenuItem(ElympicsEditorMenuPaths.BUILD_LINUX_SERVER, priority = 5)]
        private static void BuildServerLinux() => BuildTools.BuildServerLinux(BuildOptions.None);
    }
}
