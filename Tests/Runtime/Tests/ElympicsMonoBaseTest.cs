using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
namespace Elympics.Tests
{
    public abstract class ElympicsMonoBaseTest : IPrebuildSetup
    {
        public abstract string SceneName { get; }


        public void Setup()
        {
#if UNITY_EDITOR
            ElympicsLogger.Log("Setup configs");
            var config = ElympicsConfig.Load();
            if (config == null)
            {
                if (!Directory.Exists(ElympicsConfig.ElympicsResourcesPath))
                {
                    ElympicsLogger.Log("Creating Elympics resources directory...");
                    _ = Directory.CreateDirectory(ElympicsConfig.ElympicsResourcesPath);
                }

                var newConfig = ScriptableObject.CreateInstance<ElympicsConfig>();

                const string resourcesDirectory = "Assets/Resources/";
                var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(resourcesDirectory + ElympicsConfig.PathInResources + ".asset");
                AssetDatabase.CreateAsset(newConfig, assetPathAndName);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                config = newConfig;
            }
            var currentConfigs = Resources.LoadAll<ElympicsGameConfig>("Elympics");
            if (currentConfigs is null
                || currentConfigs.Length == 0)
            {
                var gameConfig = ScriptableObject.CreateInstance<ElympicsGameConfig>();
                if (!Directory.Exists(ElympicsConfig.ElympicsResourcesPath))
                {
                    ElympicsLogger.Log("Creating Elympics Resources directory...");
                    _ = Directory.CreateDirectory(ElympicsConfig.ElympicsResourcesPath);
                    ElympicsLogger.Log("Elympics Resources directory created successfully.");
                }

                AssetDatabase.CreateAsset(gameConfig, ElympicsConfig.ElympicsResourcesPath + "/ElympicsGameConfig.asset");
                config.availableGames = new()
                {
                    gameConfig
                };
                config.currentGame = 0;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            if (config.availableGames == null
                || config.availableGames.Count == 0)
            {
                var games = config.availableGames ?? new List<ElympicsGameConfig>();
                games.Add(currentConfigs![0]);
                config.availableGames = games;
                config.currentGame = 0;
            }
            ElympicsLogger.Log($"Current test elympicsConfig has {config.availableGames.Count} games and current game index is {config.currentGame}");
            var currentScenes = EditorBuildSettings.scenes.ToList();
            if (currentScenes.Any(x => x.path.Contains(SceneName)))
                return;

            var guids = AssetDatabase.FindAssets(SceneName + " t:Scene");
            if (guids.Length != 1)
                throw new ArgumentException($"There cannot be more than 1 {SceneName} scene asset.");

            var scene = AssetDatabase.GUIDToAssetPath(guids[0]);
            var editorBuildSettingScene = new EditorBuildSettingsScene(scene, true);
            currentScenes.Add(editorBuildSettingScene);
            EditorBuildSettings.scenes = currentScenes.ToArray();
#endif

        }
    }
}
