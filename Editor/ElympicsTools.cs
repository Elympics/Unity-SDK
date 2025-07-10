using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
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
        public static void ResetIds()
        {
            NetworkIdEnumerator.Instance.Reset();

            var behaviours = SceneObjectsFinder.FindObjectsOfType<ElympicsBehaviour>(SceneManager.GetActiveScene(), true);

            ReassignNetworkIdsPreservingOrder(behaviours);
            AssignNetworkIdsForNewBehaviours(behaviours);
            CheckIfThereIsNoRepetitionsInNetworkIds(behaviours);
        }

        private static void ReassignNetworkIdsPreservingOrder(List<ElympicsBehaviour> behaviours)
        {
            var sortedBehaviours = new List<ElympicsBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour.NetworkId != ElympicsBehaviour.UndefinedNetworkId && !behaviour.forceNetworkId)
                    sortedBehaviours.Add(behaviour);
            }

            sortedBehaviours.Sort((x, y) => x.NetworkId.CompareTo(y.NetworkId));

            foreach (var behaviour in sortedBehaviours)
                AssignNextNetworkId(behaviour);
        }

        private static void AssignNextNetworkId(ElympicsBehaviour behaviour) => behaviour.UpdateSerializedNetworkId();

        private static void AssignNetworkIdsForNewBehaviours(List<ElympicsBehaviour> behaviours)
        {
            foreach (var behaviour in behaviours)
            {
                if (behaviour.NetworkId != ElympicsBehaviour.UndefinedNetworkId)
                    continue;

                AssignNextNetworkId(behaviour);
            }
        }

        private static void CheckIfThereIsNoRepetitionsInNetworkIds(List<ElympicsBehaviour> behaviours)
        {
            var behaviourNames = new Dictionary<int, string>();
            foreach (var behaviour in behaviours)
            {
                var networkId = behaviour.NetworkId;
                if (behaviourNames.TryGetValue(networkId, out var previousBehaviourName))
                {
                    ElympicsLogger.LogError($"Repeated network ID: {networkId} "
                        + $"(in object {behaviour.gameObject.name})!\n"
                        + $"Already used in object {previousBehaviourName}.");
                    continue;
                }

                behaviourNames.Add(networkId, behaviour.gameObject.name);
            }
        }

        private static ElympicsConfig CreateNewConfig()
        {
            if (!Directory.Exists(ElympicsConfig.ElympicsResourcesPath))
            {
                ElympicsLogger.Log("Creating Elympics resources directory...");
                _ = Directory.CreateDirectory(ElympicsConfig.ElympicsResourcesPath);
            }

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
        private static void BuildServerWindows() => BuildTools.BuildServerWindows();

        [MenuItem(ElympicsEditorMenuPaths.BUILD_LINUX_SERVER, priority = 5)]
        private static void BuildServerLinux() => BuildTools.BuildServerLinux();
    }
}
