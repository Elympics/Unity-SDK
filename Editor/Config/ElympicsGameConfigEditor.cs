using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Elympics.SnapshotAnalysis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elympics.Editor
{
    [CustomEditor(typeof(ElympicsGameConfig))]
    internal partial class ElympicsGameConfigEditor : UnityEditor.Editor
    {
        private const int MaxTicks = 128;
        private const int MinTicks = 1;
        private const int MaxLagInMs = 2000;
        private const int MinInputLagTicks = 1;
        private const int MaxInputLagMs = 500;
        private const int UnitLabelWidth = 30;
        private const float MinTickRateFactor = 0.2f;
        private const float DefaultTickRateFactor = 1;
        private const float MaxTickRateFactor = 5;

        private ElympicsGameConfig _config;

        private SerializedProperty _gameName;
        private SerializedProperty _gameId;
        private SerializedProperty _gameVersion;
        private SerializedProperty _players;
        private SerializedProperty _gameplaySceneAsset;
        private SerializedProperty _gameplayScene;

        private SerializedProperty _botsInServer;

        private SerializedProperty _useWeb;
        private SerializedProperty _connectionConfig;

        private SerializedProperty _ticksPerSecond;
        private SerializedProperty _minTickRateFactor;
        private SerializedProperty _maxTickRateFactor;
        private SerializedProperty _snapshotSendingPeriodInTicks;
        private SerializedProperty _inputToSendBufferSize;
        private SerializedProperty _inputLagTicks;
        private SerializedProperty _maxAllowedLagInTicks;
        private SerializedProperty _forceJumpThresholdInTicks;
        private SerializedProperty _prediction;
        private SerializedProperty _predictionLimitInTicks;

        private SerializedProperty _detailedNetworkLog;
        private SerializedProperty _mode;
        private SerializedProperty _halfRemoteMode;
        private SerializedProperty _ipForHalfRemoteMode;
        private SerializedProperty _tcpPortForHalfRemoteMode;
        private SerializedProperty _webPortForHalfRemoteMode;
        private SerializedProperty _recordSnapshots;
        private SerializedProperty _snapshotFilePath;
        private SerializedProperty _playerIndexForHalfRemoteMode;
        private SerializedProperty _testMatchDataQueue;
        private SerializedProperty _testMatchDataRegion;
        private SerializedProperty _testPlayers;

        private GUIStyle _indentation;
        private Stack<float> _labelWidthStack;

        private bool _verifyGameScenePath;
        private bool _showGameSetup;
        private bool _currentGameVersionIsUploadedStatusUnknown = true;

        private void OnEnable()
        {
            try
            {
                _config = serializedObject.targetObject as ElympicsGameConfig;

                CurrentGameVersionUploadedToTheCloudStatus.CheckingIfGameVersionIsUploadedChanged += (isCheckingGameVersions) => _currentGameVersionIsUploadedStatusUnknown = isCheckingGameVersions;
                CurrentGameVersionUploadedToTheCloudStatus.Initialize(_config);
            }
            catch (Exception)
            {
                return;
            }

            _gameName = serializedObject.FindProperty("gameName");
            _gameId = serializedObject.FindProperty("gameId");
            _gameVersion = serializedObject.FindProperty("gameVersion");
            _players = serializedObject.FindProperty("players");
            _gameplaySceneAsset = serializedObject.FindProperty("gameplaySceneAsset");
            _gameplayScene = serializedObject.FindProperty("gameplayScene");

            _botsInServer = serializedObject.FindProperty("botsInServer");

            _useWeb = serializedObject.FindProperty("useWeb");
            _connectionConfig = serializedObject.FindProperty("connectionConfig");

            _ticksPerSecond = serializedObject.FindProperty("ticksPerSecond");
            _minTickRateFactor = serializedObject.FindProperty("minClientTickRateFactor");
            _maxTickRateFactor = serializedObject.FindProperty("maxClientTickRateFactor");
            _snapshotSendingPeriodInTicks = serializedObject.FindProperty("snapshotSendingPeriodInTicks");
            _inputToSendBufferSize = serializedObject.FindProperty("inputToSendBufferSize");
            _inputLagTicks = serializedObject.FindProperty("inputLagTicks");
            _maxAllowedLagInTicks = serializedObject.FindProperty("maxAllowedLagInTicks");
            _forceJumpThresholdInTicks = serializedObject.FindProperty("forceJumpThresholdInTicks");
            _prediction = serializedObject.FindProperty("prediction");
            _predictionLimitInTicks = serializedObject.FindProperty("predictionLimitInTicks");

            _detailedNetworkLog = serializedObject.FindProperty("detailedNetworkLog");
            _mode = serializedObject.FindProperty("mode");
            _halfRemoteMode = serializedObject.FindProperty("halfRemoteMode");
            _ipForHalfRemoteMode = serializedObject.FindProperty("ipForHalfRemoteMode");
            _tcpPortForHalfRemoteMode = serializedObject.FindProperty("tcpPortForHalfRemoteMode");
            _webPortForHalfRemoteMode = serializedObject.FindProperty("webPortForHalfRemoteMode");
            _recordSnapshots = serializedObject.FindProperty("recordSnapshots");
            _snapshotFilePath = serializedObject.FindProperty("snapshotFilePath");
            _playerIndexForHalfRemoteMode = serializedObject.FindProperty("playerIndexForHalfRemoteMode");
            var testMatchData = serializedObject.FindProperty("testMatchData");
            _testMatchDataQueue = testMatchData.FindPropertyRelative(nameof(ElympicsGameConfig.InitialMatchData.queueName));
            _testMatchDataRegion = testMatchData.FindPropertyRelative(nameof(ElympicsGameConfig.InitialMatchData.regionName));
            _testPlayers = serializedObject.FindProperty("testPlayers");

            _indentation = new GUIStyle { margin = new RectOffset(10, 0, 0, 0) };
            _labelWidthStack = new Stack<float>();

            _verifyGameScenePath = true;
        }

        private void OnDestroy() => CurrentGameVersionUploadedToTheCloudStatus.Disable();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var summaryLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Italic, wordWrap = true };
            var cachedWordWrap = EditorStyles.label.wordWrap;
            EditorStyles.label.wordWrap = true;

            EditorGUI.BeginDisabledGroup(true);
            _showGameSetup = EditorGUILayout.Foldout(_showGameSetup, "Game Setup");
            EditorGUI.EndDisabledGroup();

            if (_showGameSetup)
            {
                EditorGUI.BeginDisabledGroup(true);
                _ = EditorGUILayout.PropertyField(_gameName, new GUIContent("Game name"));
                _ = EditorGUILayout.PropertyField(_gameId, new GUIContent("Game id"));
                _ = EditorGUILayout.PropertyField(_gameVersion, new GUIContent("Game version"));
                _ = EditorGUILayout.PropertyField(_players, new GUIContent("Players number"));
                _ = serializedObject.ApplyModifiedProperties();
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Separator();
            }
            if (!Guid.TryParse(_config.gameId, out _))
                EditorGUILayout.HelpBox(Label_GameIdIsNotGuidError, MessageType.Error);

            DrawGameplayScene();

            BeginSection("Server");
            _ = EditorGUILayout.PropertyField(_botsInServer, new GUIContent("Bots inside server"));
            EditorGUILayout.LabelField(Label_BotsInsideServerSummary, summaryLabelStyle);
            _ = serializedObject.ApplyModifiedProperties();
            EndSection();

            BeginSection("Client");
            DrawUseWeb(summaryLabelStyle);
            _ = EditorGUILayout.PropertyField(_connectionConfig, new GUIContent("Client connection config"));
            EditorGUILayout.Space();

            _ticksPerSecond.intValue = TickSlider("Ticks per second", _ticksPerSecond.intValue, MinTicks, MaxTicks);
            _minTickRateFactor.floatValue = FactorSlider(new GUIContent("Min factor", "How much client can decrease the tick rate to avoid getting too far ahead of the server"), _minTickRateFactor.floatValue, MinTickRateFactor, DefaultTickRateFactor);
            _maxTickRateFactor.floatValue = FactorSlider(new GUIContent("Max factor", "How much client can increase the tick rate to keep up with the server"), _maxTickRateFactor.floatValue, DefaultTickRateFactor, MaxTickRateFactor);
            var text = _minTickRateFactor.floatValue == _maxTickRateFactor.floatValue ? $"{_ticksPerSecond.intValue} ticks" : $"From {Mathf.Floor(_minTickRateFactor.floatValue * _ticksPerSecond.intValue)} To {Mathf.Ceil(_maxTickRateFactor.floatValue * _ticksPerSecond.intValue)} ticks";
            EditorGUILayout.LabelField(new GUIContent("Client Tick per second", "This applies only to client. The exact value will be determined each tick based on network conditions"), new GUIContent(text), new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight });
            _snapshotSendingPeriodInTicks.intValue = TickSlider("Send snapshot every", _snapshotSendingPeriodInTicks.intValue, MinTicks, _ticksPerSecond.intValue);
            _inputLagTicks.intValue = TickSliderConvertedToMs(new GUIContent("Input lag", "The amount of time clients should stay ahead of the server"), _inputLagTicks.intValue, MinInputLagTicks, MsToTicks(MaxInputLagMs));
            _inputToSendBufferSize.intValue = IntSliderWithUnit(new GUIContent("Input Buffer Size", "Number of ticks from which input shuld be locally stored by client and sent to server each time to decrease the risk of loosing input due to network conditions"), _inputToSendBufferSize.intValue, 1, 100, "ticks");
            _maxAllowedLagInTicks.intValue = Math.Max(TickSliderConvertedToMs("Max allowed lag", _maxAllowedLagInTicks.intValue, 0, MsToTicks(MaxLagInMs)), 0);
            _forceJumpThresholdInTicks.intValue = TickSliderConvertedToMs(new GUIContent("Force jump threshold", "How far behind the desired tick client has to be to force a jump to that tick"), _forceJumpThresholdInTicks.intValue, 0, _inputLagTicks.intValue * 3);
            _ = EditorGUILayout.PropertyField(_prediction, new GUIContent("Prediction"));
            _predictionLimitInTicks.intValue = TickSliderConvertedToMs(new GUIContent("Prediction limit", "How much further ahead of server than it is supposed to client can go before it stops to wait for server"), _predictionLimitInTicks.intValue, 0, _maxAllowedLagInTicks.intValue);
            _ = serializedObject.ApplyModifiedProperties();
            EditorGUILayout.LabelField(new GUIContent("Total prediction limit", "With input lag and snapshot sending period included"), new GUIContent($"{TicksToMs(_config.TotalPredictionLimitInTicks)} ms"), new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight });
            EndSection();
            EditorGUILayout.Separator();

            BeginSection("Development");
            _ = EditorGUILayout.PropertyField(_detailedNetworkLog, new GUIContent("Detailed network log", "The log contains errors and warnings to differentiate between slight and serious throttle"));

            var previousMode = _mode.enumValueIndex;
            _ = EditorGUILayout.PropertyField(_mode, new GUIContent("Mode"));
            if (previousMode != _mode.enumValueIndex)
            {
                if ((ElympicsGameConfig.GameplaySceneDebugModeEnum)_mode.enumValueIndex == ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote && !Application.runInBackground)
                    ElympicsLogger.LogError("Development Mode is set to Half Remote but PlayerSettings "
                        + "\"Run In Background\" option is false. Network simulation will not be performed in "
                        + "out-of-focus Editor windows. Please make sure that PlayerSettings \"Run In Background\" "
                        + "option is set to true.");
            }

            switch ((ElympicsGameConfig.GameplaySceneDebugModeEnum)_mode.enumValueIndex)
            {
                case ElympicsGameConfig.GameplaySceneDebugModeEnum.LocalPlayerAndBots:
                    EditorGUILayout.LabelField("Run the server, a single player and bots locally with no networking. Good for anything outside of gameplay, such as UI, graphics and sound design.", summaryLabelStyle);
                    EditorGUILayout.HelpBox("This mode is not fit for gameplay development!", MessageType.Warning, true);
                    break;
                case ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote:
                    EditorGUILayout.LabelField("Run the server, players and bots separately with simulated networking. The mock network can simulate many connection types. Best for gameplay development, provides a semi-realistic game behavior with relatively quick testing cycles. You can also test on multiple devices by providing a non-local server address. A single Unity instance can host either a server, user or bot, use ParrelSync to create more instances.", summaryLabelStyle);
                    EditorGUILayout.HelpBox("This mode is only a simulation of production environment!", MessageType.Warning, true);
                    DrawHalfRemote();
                    break;
                case ElympicsGameConfig.GameplaySceneDebugModeEnum.DebugOnlinePlayer:
                    EditorGUILayout.LabelField("Connect as a player to production server (which has to be uploaded beforehand). Realistic environment, occasionally better stack trace. Great for finalizing a feature or release.", summaryLabelStyle);
                    DisplayGameVersionInfo();
                    DrawInitialMatchData();
                    break;
                case ElympicsGameConfig.GameplaySceneDebugModeEnum.SnapshotReplay:
                    EditorGUILayout.LabelField("Replay previously recorded match using snapshots from a file.", summaryLabelStyle);
                    DrawSnapshotReplay();
                    break;
                case ElympicsGameConfig.GameplaySceneDebugModeEnum.SinglePlayer:
                    EditorGUILayout.LabelField("Play locally acting as server and client at the same time.", summaryLabelStyle);
                    break;
                default:
                    break;
            }

            DrawInitialUserDatas();
            _ = serializedObject.ApplyModifiedProperties();
            EndSection();

            EditorStyles.label.wordWrap = cachedWordWrap;
        }

        private void DrawSnapshotReplay()
        {
            _ = EditorGUILayout.PropertyField(_snapshotFilePath, new GUIContent("Snapshot file path", "Path to a file or a folder where snapshots are stored."));

            if (string.IsNullOrWhiteSpace(_snapshotFilePath.stringValue))
            {
                EditorGUILayout.HelpBox("Snapshot file path is required.", MessageType.Error, true);
            }
            else if (_snapshotFilePath.stringValue.EndsWith(Path.DirectorySeparatorChar) || _snapshotFilePath.stringValue.EndsWith(Path.AltDirectorySeparatorChar))
            {
                if (!Directory.Exists(_snapshotFilePath.stringValue))
                    EditorGUILayout.HelpBox("Snapshot file path is invalid or points to a folder that does not exist or can't be accessed.", MessageType.Error, true);
            }
            else if (!File.Exists(_snapshotFilePath.stringValue) && !File.Exists(_snapshotFilePath.stringValue + EditorSnapshotAnalysisCollector.DefaultFileExtension))
            {
                EditorGUILayout.HelpBox("Snapshot file path is invalid or points to a file that does not exist or can't be accessed.", MessageType.Error, true);
            }
        }

        private void DisplayGameVersionInfo()
        {
            if (_currentGameVersionIsUploadedStatusUnknown)
                EditorGUILayout.HelpBox($"Checking if current game version is uploaded to the Elympics cloud...", MessageType.Info, true);
            else if (!CurrentGameVersionUploadedToTheCloudStatus.IsVersionUploaded)
                EditorGUILayout.HelpBox($"Current game version is not uploaded to the Elympics cloud! Upload your game first in \"{ElympicsEditorMenuPaths.MANAGE_GAMES_IN_ELYMPICS}\" games in Elympics window!", MessageType.Error, true);
        }

        private void BeginSection(string header)
        {
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            _ = EditorGUILayout.BeginVertical(_indentation);
            _labelWidthStack.Push(EditorGUIUtility.labelWidth);
            EditorGUIUtility.labelWidth -= _indentation.margin.left;
        }

        private void EndSection()
        {
            EditorGUIUtility.labelWidth = _labelWidthStack.Pop();
            EditorGUILayout.EndVertical();
        }

        private void DrawGameplayScene()
        {
            EditorGUI.BeginDisabledGroup(true);
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _ = EditorGUILayout.PropertyField(_gameplaySceneAsset, new GUIContent("Gameplay scene"));
            if (EditorGUI.EndChangeCheck() || _verifyGameScenePath)
                CheckGameplaySceneAndUpdatePath();
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Open scene"))
                _ = EditorSceneManager.OpenScene(_gameplayScene.stringValue);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.EndHorizontal();
            _ = EditorGUILayout.PropertyField(_gameplayScene, new GUIContent("Gameplay scene path"));
            EditorGUI.EndDisabledGroup();
        }

        private void CheckGameplaySceneAndUpdatePath()
        {
            _verifyGameScenePath = false;

            var previousValue = _gameplaySceneAsset.GetValue();
            _ = serializedObject.ApplyModifiedProperties();

            var newValue = _gameplaySceneAsset.GetValue();
            if (newValue is not SceneAsset scene)
            {
                _gameplaySceneAsset.SetValue(previousValue);
                _ = serializedObject.ApplyModifiedProperties();
                return;
            }

            var scenePath = AssetDatabase.GetAssetOrScenePath(scene);
            _gameplayScene.stringValue = scenePath;
        }

        private void DrawUseWeb(GUIStyle summaryLabelStyle)
        {
            if (ElympicsGameConfig.IsOverridenByWebGL())
            {
                EditorGUI.BeginDisabledGroup(true);
                _ = EditorGUILayout.Toggle("Use HTTPS/WebRTC (forced by WebGL)", ElympicsGameConfig.GetUseWeb(_useWeb.boolValue));
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                _ = EditorGUILayout.PropertyField(_useWeb, new GUIContent("Use HTTPS/WebRTC"));
                if (_useWeb.boolValue)
                    EditorGUILayout.LabelField(Label_WebClientWarning, summaryLabelStyle);
            }
        }

        private void DrawHalfRemote()
        {
            if (ElympicsGameConfig.IsOverridenInHalfRemoteByClone())
                EditorGUI.BeginDisabledGroup(true);

            const string remoteModeDescription = "Half remote mode";

            ElympicsGameConfig.HalfRemoteModeEnum halfRemoteMode;
            if (ElympicsGameConfig.IsOverridenInHalfRemoteByClone())
            {
                halfRemoteMode = (ElympicsGameConfig.HalfRemoteModeEnum)_halfRemoteMode.enumValueIndex;
                halfRemoteMode = ElympicsGameConfig.GetHalfRemoteMode(halfRemoteMode);
                _ = EditorGUILayout.EnumPopup(remoteModeDescription, halfRemoteMode);
            }
            else
            {
                _ = EditorGUILayout.PropertyField(_halfRemoteMode, new GUIContent(remoteModeDescription));
                halfRemoteMode = (ElympicsGameConfig.HalfRemoteModeEnum)_halfRemoteMode.enumValueIndex;
            }

            switch (halfRemoteMode)
            {
                case ElympicsGameConfig.HalfRemoteModeEnum.Server:
                    _ = EditorGUILayout.PropertyField(_ipForHalfRemoteMode, new GUIContent("IP Address of server"));
                    _ = EditorGUILayout.PropertyField(_tcpPortForHalfRemoteMode, new GUIContent("Port TCP server listens on"));
                    _ = EditorGUILayout.PropertyField(_webPortForHalfRemoteMode, new GUIContent("Port Web server listens on"));
                    _ = EditorGUILayout.PropertyField(_recordSnapshots, new GUIContent("Record snapshots", "Save snapshots to a file to analyze and replay them later"));

                    if (_recordSnapshots.boolValue)
                        _ = EditorGUILayout.PropertyField(_snapshotFilePath, new GUIContent("Snapshot file path", "Path to a folder or a specific file where snapshots should be saved"));

                    break;
                case ElympicsGameConfig.HalfRemoteModeEnum.Client:
                case ElympicsGameConfig.HalfRemoteModeEnum.Bot:
#pragma warning disable IDE0045
                    if (ElympicsGameConfig.IsOverridenInHalfRemoteByClone())
                        EditorGUI.EndDisabledGroup();

                    _ = EditorGUILayout.PropertyField(_ipForHalfRemoteMode, new GUIContent("IP Address of server"));
                    if (ElympicsGameConfig.GetUseWeb(_useWeb.boolValue))
                        _ = EditorGUILayout.PropertyField(_webPortForHalfRemoteMode, new GUIContent("Web port of server"));
                    else
                        _ = EditorGUILayout.PropertyField(_tcpPortForHalfRemoteMode, new GUIContent("TCP port of server"));

                    if (ElympicsGameConfig.IsOverridenInHalfRemoteByClone())
                        EditorGUI.BeginDisabledGroup(true);

                    if (ElympicsGameConfig.IsOverridenInHalfRemoteByClone())
                        _ = EditorGUILayout.IntField("Used player index", ElympicsGameConfig.GetHalfRemotePlayerIndex(_playerIndexForHalfRemoteMode.intValue));
                    else
                        _ = EditorGUILayout.PropertyField(_playerIndexForHalfRemoteMode, new GUIContent("Used player index"));
#pragma warning restore IDE0045
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (ElympicsGameConfig.IsOverridenInHalfRemoteByClone())
                EditorGUI.EndDisabledGroup();

            if (halfRemoteMode == ElympicsGameConfig.HalfRemoteModeEnum.Client)
            {
                EditorGUILayout.LabelField("Lag");
                _ = EditorGUILayout.BeginVertical(_indentation);
                _config.HalfRemoteLagConfig.DelayMs = EditorGUILayout.IntField("Delay milliseconds", _config.HalfRemoteLagConfig.DelayMs);
                _config.HalfRemoteLagConfig.PacketLoss = EditorGUILayout.FloatField("Packet loss", _config.HalfRemoteLagConfig.PacketLoss);
                _config.HalfRemoteLagConfig.JitterMs = EditorGUILayout.IntField("Jitter milliseconds (gaussian)", _config.HalfRemoteLagConfig.JitterMs);
                _config.HalfRemoteLagConfig.RandomSeed = EditorGUILayout.IntField("Random seed", _config.HalfRemoteLagConfig.RandomSeed);
                EditorGUILayout.LabelField("Preset");
                _ = EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("LAN"))
                    _config.HalfRemoteLagConfig.LoadLan();
                if (GUILayout.Button("Broadband"))
                    _config.HalfRemoteLagConfig.LoadBroadband();
                if (GUILayout.Button("Slow broadband"))
                    _config.HalfRemoteLagConfig.LoadSlowBroadband();
                if (GUILayout.Button("LTE"))
                    _config.HalfRemoteLagConfig.LoadLTE();
                if (GUILayout.Button("3G"))
                    _config.HalfRemoteLagConfig.Load3G();
                if (GUILayout.Button("Total mess"))
                    _config.HalfRemoteLagConfig.LoadTotalMess();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Separator();

                _config.ReconciliationFrequency = (ElympicsGameConfig.ReconciliationFrequencyEnum)EditorGUILayout.EnumPopup("Reconcile", _config.ReconciliationFrequency);
            }

            EditorGUILayout.Separator();
        }

        private void DrawInitialMatchData()
        {
            EditorGUILayout.LabelField("Test match data", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic });
            _ = EditorGUILayout.PropertyField(_testMatchDataQueue);
            _ = EditorGUILayout.PropertyField(_testMatchDataRegion);
            EditorGUILayout.Separator();
        }

        private void DrawInitialUserDatas()
        {
            EditorGUILayout.LabelField("Test players", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic });
            if (!_testPlayers.isArray)
                _testPlayers.SetValue(new List<ElympicsGameConfig.InitialUserData>());

            var wantedArraySize = Mathf.Clamp(_players.intValue, 1, 32);
            while (_testPlayers.arraySize < wantedArraySize)
                _testPlayers.InsertArrayElementAtIndex(_testPlayers.arraySize);

            while (_testPlayers.arraySize > wantedArraySize)
                _testPlayers.DeleteArrayElementAtIndex(_testPlayers.arraySize - 1);

            for (var i = 0; i < _testPlayers.arraySize; i++)
            {
                _ = EditorGUILayout.PropertyField(_testPlayers.GetArrayElementAtIndex(i));
                EditorGUILayout.Separator();
            }
        }

        private int TickSliderConvertedToMs(string label, int value, int left, int right) => TickSliderConvertedToMs(new GUIContent(label), value, left, right);

        private int TickSliderConvertedToMs(GUIContent content, int value, int left, int right)
        {
            var newMsValue = IntSliderWithUnit(content, TicksToMs(value), TicksToMs(left), TicksToMs(right), "ms");
            return Mathf.Clamp(MsToTicks(newMsValue), left, right);
        }

        private int TickSlider(string label, int value, int left, int right) => TickSlider(new GUIContent(label), value, left, right);

        private float FactorSlider(string label, float value, float min, float max) => FloatSliderWithUnit(new GUIContent(label), value, min, max, string.Empty);
        private float FactorSlider(GUIContent content, float value, float min, float max) => FloatSliderWithUnit(content, value, min, max, string.Empty);

        private int TickSlider(GUIContent content, int value, int left, int right) => IntSliderWithUnit(content, value, left, right, "ticks");

        private int IntSliderWithUnit(GUIContent content, int value, int left, int right, string unit)
        {
            _ = EditorGUILayout.BeginHorizontal();
            var newValue = EditorGUILayout.IntSlider(content, value, left, right, GUILayout.MaxWidth(float.MaxValue));
            EditorGUILayout.LabelField(unit, GUILayout.Width(UnitLabelWidth));
            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        private float FloatSliderWithUnit(GUIContent content, float value, float left, float right, string unit)
        {
            _ = EditorGUILayout.BeginHorizontal();
            var newValue = EditorGUILayout.Slider(content, value, left, right, GUILayout.MaxWidth(float.MaxValue));
            EditorGUILayout.LabelField(unit, GUILayout.Width(UnitLabelWidth));
            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        private int TicksToMs(int ticks) => (int)Math.Round(ticks * 1000.0 / _ticksPerSecond.intValue);

        private int MsToTicks(int milliseconds) => (int)Math.Round(_ticksPerSecond.intValue * milliseconds / 1000.0);

        [CustomPropertyDrawer(typeof(ElympicsGameConfig.InitialUserData))]
        public class InitialUserDataPropertyDrawer : PropertyDrawer
        {
            private static readonly string[] GameEngineDataShowTypes = { "Base64", "String" };

            private readonly Dictionary<int, bool> _showPlayers = new();
            private readonly Dictionary<int, int> _gameEngineDataShowType = new();

            private static readonly Regex NumberRegex = new(@"\d+");

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var index = int.Parse(NumberRegex.Match(property.propertyPath).Value, NumberFormatInfo.InvariantInfo);
                if (!_showPlayers.TryGetValue(index, out var showPlayer))
                    _showPlayers.Add(index, default);

                showPlayer = EditorGUI.Foldout(position, showPlayer, $"Player {index}", new GUIStyle(EditorStyles.foldout) { margin = new RectOffset(10, 0, 0, 0) });
                _showPlayers[index] = showPlayer;
                if (!showPlayer)
                    return;

                EditorGUI.indentLevel += 1;
                var userId = property.FindPropertyRelative("userId");
                var isBot = property.FindPropertyRelative("isBot");
                var botDifficulty = property.FindPropertyRelative("botDifficulty");
                var gameEngineData = property.FindPropertyRelative("gameEngineData");
                var matchmakerData = property.FindPropertyRelative("matchmakerData");

                userId.stringValue = index.ToString();

                _ = EditorGUILayout.PropertyField(isBot);
                if (isBot.boolValue)
                    _ = EditorGUILayout.PropertyField(botDifficulty);

                DrawGameEngineData(gameEngineData, index);
                DrawMatchmakerData(matchmakerData);

                EditorGUI.indentLevel -= 1;
            }

            private void DrawGameEngineData(SerializedProperty gameEngineData, int index)
            {
                if (!_gameEngineDataShowType.TryGetValue(index, out var gameEngineDataShowTypeChosen))
                    _gameEngineDataShowType.Add(index, default);

                _ = EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Game engine data");
                EditorGUI.BeginChangeCheck();
                gameEngineDataShowTypeChosen = GUILayout.Toolbar(gameEngineDataShowTypeChosen, GameEngineDataShowTypes, GUI.skin.button, GUI.ToolbarButtonSize.FitToContents);
                if (EditorGUI.EndChangeCheck())
                {
                    _gameEngineDataShowType[index] = gameEngineDataShowTypeChosen;
                    GUI.FocusControl(null);
                }

                EditorGUILayout.EndHorizontal();

                try
                {
                    var gameEngineDataValue = (byte[])gameEngineData.GetValue();
                    byte[] newGameEngineDataValue = null;
                    switch (GameEngineDataShowTypes[gameEngineDataShowTypeChosen])
                    {
                        case "Base64":
                        {
                            var gameEngineDataAsByte64 = Convert.ToBase64String(gameEngineDataValue);
                            var newGameEngineDataAsByte64 = EditorGUILayout.TextArea(gameEngineDataAsByte64, new GUIStyle(GUI.skin.textArea) { fixedHeight = 0, stretchWidth = true, stretchHeight = true });
                            newGameEngineDataValue = newGameEngineDataAsByte64.Length != 0 ? Convert.FromBase64String(newGameEngineDataAsByte64) : Array.Empty<byte>();
                            break;
                        }
                        case "String":
                        {
                            var gameEngineDataAsString = Encoding.ASCII.GetString(gameEngineDataValue);
                            var newGameEngineDataAsString = EditorGUILayout.TextArea(gameEngineDataAsString, new GUIStyle(GUI.skin.textArea) { fixedHeight = 0, stretchWidth = true, stretchHeight = true });
                            newGameEngineDataValue = newGameEngineDataAsString.Length != 0 ? Encoding.ASCII.GetBytes(newGameEngineDataAsString) : Array.Empty<byte>();
                            break;
                        }

                        default:
                            break;
                    }

                    gameEngineData.SetValue(newGameEngineDataValue);
                }
                catch (FormatException e)
                {
                    ElympicsLogger.LogWarning(e.Message);
                }
            }

            private static void DrawMatchmakerData(SerializedProperty matchmakerData)
            {
                var mmDataValue = (float[])matchmakerData.GetValue();
                EditorGUI.BeginChangeCheck();
                var newMmDataValueString = EditorGUILayout.TextField("Matchmaker data", string.Join(" ", mmDataValue.Select(x => x.ToString(CultureInfo.InvariantCulture))));
                if (EditorGUI.EndChangeCheck())
                {
                    var newMmDataValue = newMmDataValueString.Length != 0 ? newMmDataValueString.Split(' ').Select(x => float.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : 0f).ToArray() : Array.Empty<float>();
                    matchmakerData.SetValue(newMmDataValue);
                }
            }
        }
    }
}
