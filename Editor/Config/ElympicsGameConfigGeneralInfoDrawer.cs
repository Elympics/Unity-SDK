using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elympics
{
    internal class ElympicsGameConfigGeneralInfoDrawer
    {
        private ElympicsGameConfig _gameConfig;
        private SerializedObject _gameConfigSo;

        private SerializedProperty _gameName;
        private SerializedProperty _gameId;
        private SerializedProperty _gameVersion;
        private SerializedProperty _players;
        private SerializedProperty _gameplayScene;
        private SerializedProperty _gameplaySceneAsset;

        private readonly CustomInspectorDrawer _customInspectorDrawer;
        private readonly Color _themeColor;

        private string _previousGameName;
        private string _previousGameId;
        private string _previousGameVersion;
        private int _previousPlayers;
        private Object _previousSceneAsset;
        private bool _verifyGameScenePath;

        public event System.Action DataChanged;

        private const string GameIdInvalidFormatErrorMessage = "Game Id is required to be in a Guid format!";

        public ElympicsGameConfigGeneralInfoDrawer(CustomInspectorDrawer inspectorDrawer, Color themeColor)
        {
            _customInspectorDrawer = inspectorDrawer;
            _themeColor = themeColor;
        }

        public void UpdateGameConfigProperty(ElympicsGameConfig gameConfig)
        {
            if (gameConfig == _gameConfig)
                return;

            _gameConfig = gameConfig;
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
            CheckIfFieldDataChanged();
        }

        private void CheckIfFieldDataChanged()
        {
            var dataChanged = false;
            dataChanged |= _previousGameName != _gameName.stringValue;
            dataChanged |= _previousGameId != _gameId.stringValue;
            dataChanged |= _previousGameVersion != _gameVersion.stringValue;
            dataChanged |= _previousPlayers != _players.intValue;

            if (dataChanged)
                DataChanged?.Invoke();

            _previousGameName = _gameName.stringValue;
            _previousGameId = _gameId.stringValue;
            _previousGameVersion = _gameVersion.stringValue;
            _previousPlayers = _players.intValue;
        }

        #region Game Config Settings Section

        private void DrawSettingsSection()
        {
            _customInspectorDrawer.DrawHeader(_gameName.stringValue + " settings", 20, _themeColor);
            _customInspectorDrawer.Space();

            _gameName.stringValue = _customInspectorDrawer.DrawStringField("Name", _gameName.stringValue, 0.25f, true);
            _gameId.stringValue = _customInspectorDrawer.DrawStringField("Game Id", _gameId.stringValue, 0.25f, true);
            if (!System.Guid.TryParse(_gameId.stringValue, out _))
                _customInspectorDrawer.DrawHelpBox(GameIdInvalidFormatErrorMessage, 40, MessageType.Error);

            _gameVersion.stringValue = _customInspectorDrawer.DrawStringField("Version", _gameVersion.stringValue, 0.25f, true);

            _gameplaySceneAsset.objectReferenceValue = _customInspectorDrawer.DrawSceneFieldWithOpenSceneButton(
                "Gameplay scene", _gameplaySceneAsset.objectReferenceValue, 0.25f, 0.5f, out var sceneFieldChanged,
                out var openSceneButtonPressed);
            if (sceneFieldChanged || _verifyGameScenePath)
                CheckGameplaySceneAndUpdatePath();
            if (openSceneButtonPressed)
                OpenSceneFromGameConfig();

            _ = _customInspectorDrawer.DrawStringField("Scene path", _gameplayScene.stringValue, 0.25f, false);
        }

        private void OpenSceneFromGameConfig() => _ = EditorSceneManager.OpenScene(_gameplayScene.stringValue);

        private void CheckGameplaySceneAndUpdatePath()
        {
            _verifyGameScenePath = false;
            var newSceneAsset = _gameplaySceneAsset.objectReferenceValue;

            if (newSceneAsset is not SceneAsset scene)
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
