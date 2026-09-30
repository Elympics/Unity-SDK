#nullable enable

using System;
using System.Globalization;
using System.IO;
using Elympics.SnapshotAnalysis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Elympics.Editor.Config
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
            var snapshotReplayPath = inspectorTree.Q<TextField>("snapshot-replay-path");
            var snapshotReplayError = inspectorTree.Q<HelpBox>("snapshot-replay-error");

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

            void UpdateSnapshotReplayOptions()
            {
                snapshotReplayError.SetVisible(false);
                snapshotReplayError.text = "";

                // Recording creates the file, and an http(s) URL is downloaded at launch: only an existing local replay can be checked.
                if (string.IsNullOrWhiteSpace(gameConfig.SnapshotFilePath) || gameConfig.RecordSnapshots
                    || new SnapshotReplayLaunchData(gameConfig.SnapshotFilePath).IsUrl)
                    return;
                if (gameConfig.SnapshotFilePath.EndsWith(Path.DirectorySeparatorChar) || gameConfig.SnapshotFilePath.EndsWith(Path.AltDirectorySeparatorChar))
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
