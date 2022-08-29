using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elympics
{
	internal class ElympicsGameConfigGeneralInfoDrawer
	{
		private ElympicsGameConfig _gameConfig;
		private SerializedProperty _gameConfigSp;
		private SerializedObject   _gameConfigSo;

		private SerializedProperty _gameName;
		private SerializedProperty _gameId;
		private SerializedProperty _gameVersion;
		private SerializedProperty _players;
		private SerializedProperty _gameplayScene;
		private SerializedProperty _gameplaySceneAsset;

		private readonly CustomInspectorDrawer _customInspectorDrawer;
		private readonly Color                 _themeColor;

		private Object _previousSceneAsset;
		private bool   _verifyGameScenePath;

		public ElympicsGameConfigGeneralInfoDrawer(CustomInspectorDrawer inspectorDrawer, Color themeColor)
		{
			_customInspectorDrawer = inspectorDrawer;
			_themeColor = themeColor;
		}

		public void UpdateGameConfigProperty(SerializedProperty gameConfigSp)
		{
			if (gameConfigSp.objectReferenceValue == _gameConfig)
				return;

			_gameConfigSp = gameConfigSp;
			_gameConfig = _gameConfigSp.objectReferenceValue as ElympicsGameConfig;
			_gameConfigSo = new SerializedObject(_gameConfig);

			_gameName = _gameConfigSo.FindProperty("gameName");
			_gameId = _gameConfigSo.FindProperty("gameId");
			_gameVersion = _gameConfigSo.FindProperty("gameVersion");
			_players = _gameConfigSo.FindProperty("players");
			_gameplayScene = _gameConfigSo.FindProperty("gameplayScene");
			_gameplaySceneAsset = _gameConfigSo.FindProperty("gameplaySceneAsset");

			_previousSceneAsset = _gameplaySceneAsset?.objectReferenceValue;
			_verifyGameScenePath = true;
		}

		public void ApplyModifications() => _gameConfigSo.ApplyModifiedProperties();

		public void DrawGeneralGameConfigInfo()
		{
			DrawSettingsSection();
		}

		#region Game Config Settings Section

		private void DrawSettingsSection()
		{
			_customInspectorDrawer.DrawHeader(_gameName.stringValue + " settings", 20, _themeColor);
			_customInspectorDrawer.Space();

			_gameName.stringValue = _customInspectorDrawer.DrawStringField("Name", _gameName.stringValue, 0.25f, true);
			_gameId.stringValue = _customInspectorDrawer.DrawStringField("Game Id", _gameId.stringValue, 0.25f, true);
			_gameVersion.stringValue = _customInspectorDrawer.DrawStringField("Version", _gameVersion.stringValue, 0.25f, true);
			_players.intValue = _customInspectorDrawer.DrawIntField("Players Count", _players.intValue, 0.25f, true);

			_gameplaySceneAsset.objectReferenceValue = _customInspectorDrawer.DrawSceneFieldWithOpenSceneButton("Gameplay scene", _gameplaySceneAsset.objectReferenceValue, 0.25f, 0.5f, out var sceneFieldChanged, out var openSceneButtonPressed);
			if (sceneFieldChanged || _verifyGameScenePath)
				CheckGameplaySceneAndUpdatePath();
			if (openSceneButtonPressed)
				OpenSceneFromGameConfig();

			_customInspectorDrawer.DrawStringField("Scene path", _gameplayScene.stringValue, 0.25f, false);
		}

		private void OpenSceneFromGameConfig()
		{
			EditorSceneManager.OpenScene(_gameplayScene.stringValue);
		}

		private void CheckGameplaySceneAndUpdatePath()
		{
			_verifyGameScenePath = false;
			var newSceneAsset = _gameplaySceneAsset.objectReferenceValue;

			if (!(newSceneAsset is SceneAsset scene))
			{
				_gameplaySceneAsset.objectReferenceValue = _previousSceneAsset;
				return;
			}

			_previousSceneAsset = _gameplaySceneAsset.objectReferenceValue;
			var scenePath = AssetDatabase.GetAssetOrScenePath(scene);
			_gameplayScene.stringValue = scenePath;
		}

		#endregion
	}
}
