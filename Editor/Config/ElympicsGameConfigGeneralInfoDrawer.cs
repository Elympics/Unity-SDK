using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elympics
{
	internal class ElympicsGameConfigGeneralInfoDrawer
	{
		private ElympicsGameConfig gameConfig = null;
		public  ElympicsGameConfig GameConfig => gameConfig;

		private CustomInspectorDrawer customInspectorDrawer = null;
		private Color                 themeColor            = Color.blue;

		private Object previousSceneAsset = null;
		private bool   _verifyGameScenePath;

		public ElympicsGameConfigGeneralInfoDrawer(CustomInspectorDrawer inspectorDrawer, Color themeColor)
		{
			this.customInspectorDrawer = inspectorDrawer;
			this.themeColor = themeColor;
		}

		public void SetGameConfigProperty(ElympicsGameConfig gameConfig)
		{
			this.gameConfig = gameConfig;

			previousSceneAsset = gameConfig.gameplaySceneAsset;
			_verifyGameScenePath = true;
		}

		public void DrawGeneralGameConfigInfo()
		{
			DrawSettingsSection();
		}

		#region Game Config Settings Section

		private void DrawSettingsSection()
		{
			customInspectorDrawer.DrawHeader(gameConfig.GameName + " settings", 20, themeColor);
			customInspectorDrawer.Space();

			gameConfig.gameName = customInspectorDrawer.DrawStringField("Name", gameConfig.GameName, 0.25f, true);
			gameConfig.gameId = customInspectorDrawer.DrawStringField("Game Id", gameConfig.GameId, 0.25f, true);
			gameConfig.gameVersion = customInspectorDrawer.DrawStringField("Version", gameConfig.GameVersion, 0.25f, true);
			gameConfig.players = customInspectorDrawer.DrawIntField("Players Count", gameConfig.Players, 0.25f, true);

			gameConfig.gameplaySceneAsset = customInspectorDrawer.DrawSceneFieldWithOpenSceneButton("Gameplay scene", gameConfig.gameplaySceneAsset, 0.25f, 0.5f, out bool sceneFieldChanged, out bool openSceneButtonPressed);
			if (sceneFieldChanged || _verifyGameScenePath)
				CheckGameplaySceneAndUpdatePath();
			if (openSceneButtonPressed)
				OpenSceneFromGameConfig();

			customInspectorDrawer.DrawStringField("Scene path", gameConfig.GameplayScene, 0.25f, false);
		}

		private void OpenSceneFromGameConfig()
		{
			EditorSceneManager.OpenScene(gameConfig.gameplayScene);
		}

		private void CheckGameplaySceneAndUpdatePath()
		{
			_verifyGameScenePath = false;
			var newSceneAsset = gameConfig.gameplaySceneAsset;

			if (!(newSceneAsset is SceneAsset scene))
			{
				gameConfig.gameplaySceneAsset = previousSceneAsset;
				return;
			}

			previousSceneAsset = gameConfig.gameplaySceneAsset;
			var scenePath = AssetDatabase.GetAssetOrScenePath(scene);
			gameConfig.gameplayScene = scenePath;
		}

		#endregion
	}
}
