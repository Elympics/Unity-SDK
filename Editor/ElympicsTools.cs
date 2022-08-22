using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace Elympics
{
	public static class ElympicsTools
	{
		[MenuItem(ElympicsEditorMenuPaths.MANAGE_GAMES_IN_ELYMPICS, priority = 1)]
		private static void OpenManageGamesInElympicsWindow()
		{
			var elympicsConfig = LoadOrCreateConfig();
			var serializedElympicsConfig = new SerializedObject(elympicsConfig);

			var elympicsApiEndpoint = serializedElympicsConfig.FindProperty("elympicsApiEndpoint");
			var elympicsLobbyEndpoint = serializedElympicsConfig.FindProperty("elympicsLobbyEndpoint");
			var elympicsGameServersEndpoint = serializedElympicsConfig.FindProperty("elympicsGameServersEndpoint");
			var currentGameIndex = serializedElympicsConfig.FindProperty("currentGame");
			var availableGames = serializedElympicsConfig.FindProperty("availableGames");

			ManageGamesInElympicsWindow.ShowWindow(serializedElympicsConfig, currentGameIndex, availableGames, elympicsApiEndpoint, elympicsLobbyEndpoint, elympicsGameServersEndpoint);
		}

		private static ElympicsConfig LoadOrCreateConfig()
		{
			var config = ElympicsConfig.Load();
			if (config == null)
			{
				config = CreateNewConfig();
			}

			return config;
		}

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

		private static void AssignNextNetworkId(ElympicsBehaviour behaviour)
		{
			behaviour.UpdateSerializedNetworkId();
		}

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
			var networkIds = new HashSet<int>();
			foreach (var behaviour in behaviours)
			{
				if (networkIds.Contains(behaviour.NetworkId))
				{
					Debug.LogError($"Repetition for network id {behaviour.NetworkId} in {behaviour.gameObject.name} {behaviour.GetType().Name}");
					continue;
				}

				networkIds.Add(behaviour.NetworkId);
			}
		}

		private static ElympicsConfig CreateNewConfig()
		{
			if (!Directory.Exists(ElympicsConfig.ELYMPICS_RESOURCES_PATH))
			{
				Debug.Log("Creating Elympics resources directory...");
				Directory.CreateDirectory(ElympicsConfig.ELYMPICS_RESOURCES_PATH);
			}

			var newConfig = ScriptableObject.CreateInstance<ElympicsConfig>();

			const string resourcesDirectory = "Assets/Resources/";
			// TODO: there is probably some hack possible to get path to current Elympics directory
			var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(resourcesDirectory + ElympicsConfig.PATH_IN_RESOURCES + ".asset");
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
