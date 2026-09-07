#nullable enable

using System;
using System.Globalization;
using System.IO;
using Elympics.Core.Logger;
using Elympics.SnapshotAnalysis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Elympics.Editor
{
    [CustomEditor(typeof(ElympicsGameConfig))]
    internal class ElympicsGameConfigEditor : UnityEditor.Editor
    {
        private const int DataChangedDebounceMs = 500;

        public VisualTreeAsset? inspectorUxml;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            if (inspectorUxml != null)
                root.Add(PrepareInspectorTree(inspectorUxml));
            return root;
        }

        private VisualElement PrepareInspectorTree(VisualTreeAsset sourceTree)
        {
            VisualElement inspectorTree = sourceTree.CloneTree();

            var gameConfig = (ElympicsGameConfig)serializedObject.targetObject;

            var gameName = inspectorTree.Q<TextField>("game-name");
            var gameId = inspectorTree.Q<TextField>("game-id");
            var gameIdErrorBox = inspectorTree.Q<HelpBox>("game-id-error");
            var gameVersion = inspectorTree.Q<TextField>("game-version");
            var maxPlayers = inspectorTree.Q<SliderInt>("max-players");

            var scenePath = inspectorTree.Q<TextField>("scene-path");
            var sceneAsset = inspectorTree.Q<ObjectField>("scene-object");
            var openScene = inspectorTree.Q<Button>("open-scene-button");

            var ticksPerSecond = inspectorTree.Q<SliderInt>("ticks-per-second");
            var minClientTickRateFactor = inspectorTree.Q<Slider>("min-client-tick-rate-factor");
            var maxClientTickRateFactor = inspectorTree.Q<Slider>("max-client-tick-rate-factor");
            var tpsLabel = inspectorTree.Q<Label>("tick-rate-summary");
            var snapshotSendingInterval = inspectorTree.Q<SliderInt>("snapshot-sending-interval");
            var inputLag = inspectorTree.Q<SliderInt>("input-lag");

            var predictionLimit = inspectorTree.Q<SliderInt>("prediction-limit");
            var totalPredictionLimit = inspectorTree.Q<Label>("total-prediction-limit");

            var debugMode = inspectorTree.Q<EnumField>("debug-mode");
            var debugModeSummary = inspectorTree.Q<Label>("debug-mode-summary");
            var debugModeWarning = inspectorTree.Q<HelpBox>("debug-mode-warning");
            var halfRemoteOptions = inspectorTree.Q<GroupBox>("half-remote-options");
            var halfRemoteMode = inspectorTree.Q<EnumField>("half-remote-mode");
            var halfRemoteClientOptions = inspectorTree.Q<GroupBox>("client-half-remote-options");
            var halfRemoteRecordSnapshot = inspectorTree.Q<Toggle>("half-remote-record-snapshot");
            foreach (var (id, loader) in new (string, Action<HalfRemoteLagConfig>)[]
                     {
                         ("lag-preset-lan", c => c.LoadLan()),
                         ("lag-preset-broadband", c => c.LoadBroadband()),
                         ("lag-preset-slow-broadband", c => c.LoadSlowBroadband()),
                         ("lag-preset-lte", c => c.LoadLTE()),
                         ("lag-preset-3g", c => c.Load3G()),
                         ("lag-preset-total-mess", c => c.LoadTotalMess()),
                     })
                inspectorTree.Q<Button>(id).clicked += () => loader(gameConfig.HalfRemoteLagConfig);
            var debugOnlineOptions = inspectorTree.Q<GroupBox>("debug-online-options");
            var debugOnlineSpinner = inspectorTree.Q<HelpBox>("debug-online-uploading-spinner");
            var debugOnlineError = inspectorTree.Q<HelpBox>("debug-online-not-uploaded");
            var snapshotReplayOptions = inspectorTree.Q<GroupBox>("snapshot-replay-options");
            var snapshotReplayPath = inspectorTree.Q<TextField>("snapshot-replay-path");
            var snapshotReplayError = inspectorTree.Q<HelpBox>("snapshot-replay-error");

            bool? isCurrentGameVersionUploaded = null;
            CurrentGameVersionUploadedToTheCloudStatus.CheckingIfGameVersionIsUploadedChanged += inProgress =>
                isCurrentGameVersionUploaded = inProgress ? null : CurrentGameVersionUploadedToTheCloudStatus.IsVersionUploaded;
            CurrentGameVersionUploadedToTheCloudStatus.Initialize(gameConfig);

            var notifyDataChanged = inspectorTree.schedule.Execute(gameConfig.ProcessElympicsConfigDataChanged);
            notifyDataChanged.Pause();

            _ = gameName.RegisterValueChangedCallback(_ => RescheduleDataChangedNotification());
            _ = gameId.RegisterValueChangedCallback(_ =>
            {
                UpdateGameIdErrorBox();
                RescheduleDataChangedNotification();
            });
            _ = gameVersion.RegisterValueChangedCallback(_ => RescheduleDataChangedNotification());
            _ = maxPlayers.RegisterValueChangedCallback(_ => RescheduleDataChangedNotification());

            _ = sceneAsset.RegisterValueChangedCallback(evt =>
            {
                var asset = (SceneAsset)evt.newValue;
                scenePath.value = asset != null
                    ? AssetDatabase.GetAssetOrScenePath(asset)
                    : string.Empty;
                UpdateSceneButton();
            });

            _ = ticksPerSecond.RegisterValueChangedCallback(_ => UpdateTicksPerSecondLabel());
            _ = ticksPerSecond.RegisterValueChangedCallback(_ => UpdateInputLagHighValue());
            _ = minClientTickRateFactor.RegisterValueChangedCallback(_ => UpdateTicksPerSecondLabel());
            _ = maxClientTickRateFactor.RegisterValueChangedCallback(_ => UpdateTicksPerSecondLabel());

            _ = snapshotSendingInterval.RegisterValueChangedCallback(_ => UpdateTotalPredictionLimitLabel());
            _ = inputLag.RegisterValueChangedCallback(_ => UpdateTotalPredictionLimitLabel());
            _ = predictionLimit.RegisterValueChangedCallback(_ => UpdateTotalPredictionLimitLabel());

            _ = halfRemoteMode.RegisterValueChangedCallback(_ => UpdateHalfRemoteModeOptions());
            _ = debugMode.RegisterValueChangedCallback(evt =>
            {
                UpdateDebugModeOptions();
                if (!Application.runInBackground && (ElympicsGameConfig.GameplaySceneDebugModeEnum)evt.newValue == ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote)
                    ElympicsLogger.LogError("Development Mode is set to Half Remote but PlayerSettings "
                        + "\"Run In Background\" option is false. Network simulation will not be performed in "
                        + "out-of-focus Editor windows. Please make sure that PlayerSettings \"Run In Background\" "
                        + "option is set to true.");
            });
            _ = halfRemoteRecordSnapshot.RegisterValueChangedCallback(_ => UpdateSnapshotReplayOptions());
            _ = snapshotReplayPath.RegisterValueChangedCallback(_ => UpdateSnapshotReplayOptions());

            openScene.clicked += () =>
            {
                var path = scenePath.value;
                if (!string.IsNullOrWhiteSpace(path))
                    _ = EditorSceneManager.OpenScene(path);
            };

            UpdateGameIdErrorBox();
            UpdateSceneButton();
            UpdateTicksPerSecondLabel();
            UpdateTotalPredictionLimitLabel();
            UpdateDebugModeOptions();
            UpdateVersionUploadStatus();
            UpdateSnapshotReplayOptions();
            UpdateInputLagHighValue();

            return inspectorTree;

            void RescheduleDataChangedNotification() => notifyDataChanged.ExecuteLater(DataChangedDebounceMs);

            void UpdateGameIdErrorBox() => gameIdErrorBox.SetVisible(!Guid.TryParse(gameId.value, out _));

            void UpdateSceneButton()
            {
                openScene.SetEnabled(gameConfig.gameplaySceneAsset != null);
            }

            void UpdateTicksPerSecondLabel()
            {
                var minTps = Math.Round(gameConfig.MinTickRate, 2).ToString(CultureInfo.InvariantCulture);
                var maxTps = Math.Round(gameConfig.MaxTickRate, 2).ToString(CultureInfo.InvariantCulture);
                tpsLabel.text = $"Client ticks per second: {(minTps != maxTps ? $"from {minTps} to {maxTps}" : $"{minTps}")} ticks";
            }

            void UpdateTotalPredictionLimitLabel()
            {
                var totalLimit = (int)Math.Round(gameConfig.TotalPredictionLimitInTicks * gameConfig.TickDuration * 1000);
                totalPredictionLimit.text = $"Total prediction limit: {totalLimit} ms";
            }

            void UpdateDebugModeOptions()
            {
                debugModeWarning.text = "";
                debugModeWarning.SetVisible(false);
                halfRemoteOptions.SetVisible(false);
                debugOnlineOptions.SetVisible(false);
                snapshotReplayOptions.SetVisible(false);
                switch (gameConfig.GameplaySceneDebugMode)
                {
                    case ElympicsGameConfig.GameplaySceneDebugModeEnum.LocalPlayerAndBots:
                        debugModeSummary.text = "Run the server, a single player and bots locally with no networking. Good for anything outside of gameplay, such as UI, graphics and sound design.";
                        debugModeWarning.text = "This mode is not fit for gameplay development!";
                        debugModeWarning.SetVisible(true);
                        break;
                    case ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote:
                        debugModeSummary.text = "Run the server, players and bots separately with simulated networking. The mock network can simulate many connection types. Best for gameplay development, provides a semi-realistic game behavior with relatively quick testing cycles. You can also test on multiple devices by providing a non-local server address. A single Unity instance can host either a server, user or bot, use ParrelSync to create more instances.";
                        debugModeWarning.text = "This mode is only a simulation of production environment!";
                        debugModeWarning.SetVisible(true);
                        halfRemoteOptions.SetVisible(true);
                        UpdateHalfRemoteModeOptions();
                        break;
                    case ElympicsGameConfig.GameplaySceneDebugModeEnum.DebugOnlinePlayer:
                        debugModeSummary.text = "Connect as a player to production server (which has to be uploaded beforehand). Realistic environment, occasionally better stack trace. Great for finalizing a feature or release.";
                        debugOnlineOptions.SetVisible(true);
                        break;
                    case ElympicsGameConfig.GameplaySceneDebugModeEnum.SnapshotReplay:
                        debugModeSummary.text = "Replay previously recorded match using snapshots from a file.";
                        snapshotReplayOptions.SetVisible(true);
                        halfRemoteRecordSnapshot.SetVisible(false);
                        UpdateSnapshotReplayOptions();
                        break;
                    case ElympicsGameConfig.GameplaySceneDebugModeEnum.SinglePlayer:
                        debugModeSummary.text = "Play locally acting as server and client at the same time.";
                        break;
                    default:
                        debugModeSummary.text = "";
                        debugModeSummary.SetVisible(false);
                        break;
                }
            }

            void UpdateHalfRemoteModeOptions()
            {
                var isServer = gameConfig.HalfRemoteMode == ElympicsGameConfig.HalfRemoteModeEnum.Server;
                var isClient = gameConfig.HalfRemoteMode == ElympicsGameConfig.HalfRemoteModeEnum.Client;
                snapshotReplayOptions.SetVisible(isServer);
                halfRemoteRecordSnapshot.SetVisible(isServer);
                halfRemoteClientOptions.SetVisible(isClient);
                UpdateSnapshotReplayOptions();
            }

            void UpdateVersionUploadStatus()
            {
                debugOnlineError.SetVisible(false);
                if (!isCurrentGameVersionUploaded.HasValue)
                    debugOnlineSpinner.SetVisible(true);
                else
                {
                    debugOnlineSpinner.SetVisible(false);
                    if (!isCurrentGameVersionUploaded.Value)
                        debugOnlineError.SetVisible(true);
                }
            }

            void UpdateSnapshotReplayOptions()
            {
                snapshotReplayPath.SetEnabled(true);
                snapshotReplayError.SetVisible(false);
                snapshotReplayError.text = "";

                if (gameConfig is
                    {
                        GameplaySceneDebugMode: ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote,
                        HalfRemoteMode: ElympicsGameConfig.HalfRemoteModeEnum.Server,
                        RecordSnapshots: false,
                    })
                    snapshotReplayPath.SetEnabled(false);

                if (!snapshotReplayPath.enabledSelf)
                    return;

                if (string.IsNullOrWhiteSpace(gameConfig.SnapshotFilePath))
                    SetErrorMessage("Snapshot file path is required.");
                else if (gameConfig.SnapshotFilePath.EndsWith(Path.DirectorySeparatorChar) || gameConfig.SnapshotFilePath.EndsWith(Path.AltDirectorySeparatorChar))
                {
                    if (!Directory.Exists(gameConfig.SnapshotFilePath))
                        SetErrorMessage("Snapshot file path is invalid or points to a folder that does not exist or can't be accessed.");
                }
                else if (!File.Exists(gameConfig.SnapshotFilePath) && !File.Exists(gameConfig.SnapshotFilePath + EditorSnapshotAnalysisCollector.DefaultFileExtension))
                    SetErrorMessage("Snapshot file path is invalid or points to a file that does not exist or can't be accessed.");

                void SetErrorMessage(string message)
                {
                    snapshotReplayError.text = message;
                    snapshotReplayError.SetVisible(true);
                }
            }

            void UpdateInputLagHighValue() => inputLag.highValue = gameConfig.TicksPerSecond;
        }
    }
}
