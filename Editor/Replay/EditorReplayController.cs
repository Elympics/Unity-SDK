using System;
using System.Linq;
using Elympics.SnapshotAnalysis;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Elympics.Editor.Replay
{
    internal class EditorReplayController : IReplayManipulator
    {
        private readonly ToolbarToggle _pauseToggle;
        private readonly Button _applyStateButton;
        private readonly Toggle _autoApplyStateToggle;
        private readonly Toggle _autoSelectTickToggle;

        private TickListDisplayer _tickListDisplayer;
        private TickDataDisplayer _tickDataDisplayer;
        private IReplayManipulatorClient _replayClient;

        private long _currentTick = -1;
        private long _lastTick = -1;
        private bool _ignoreAutoApply = false;

        private bool AutoApplyState => _autoApplyStateToggle.value;
        public bool AutoSelectTick => _autoSelectTickToggle.value;
        internal bool Paused => _pauseToggle.value;

        internal EditorReplayController(VisualElement root, TickListDisplayer tickListDisplayer, TickDataDisplayer tickDataDisplayer)
        {
            _tickListDisplayer = tickListDisplayer;
            _tickDataDisplayer = tickDataDisplayer;
            // pause toggle setup
            _pauseToggle = root.Q<ToolbarToggle>("pause-control");
            // because used element template contains useless element which have to be disabled - it takes up space!
            _pauseToggle.Q<VisualElement>(classes: "unity-toggle__input").style.display = DisplayStyle.None;
            _pauseToggle.SetEnabled(EditorApplication.isPlaying);
            _ = _pauseToggle.RegisterValueChangedCallback(valueChangedEvent =>
            {
                _replayClient?.SetIsPlaying(!valueChangedEvent.newValue);
                AdjustApplyStateButtonClickability();
            });

            _applyStateButton = root.Q<Button>("apply-state-button");
            _applyStateButton.SetEnabled(false);
            _applyStateButton.clicked += () => ApplyTickStateIfNeeded(_tickDataDisplayer.SelectedTick?.Snapshot, true);

            // auto apply state toggle setup
            _autoApplyStateToggle = root.Q<Toggle>("auto-apply-control");
            _ = _autoApplyStateToggle.RegisterValueChangedCallback(valueChangedEvent =>
            {
                AdjustApplyStateButtonClickability();

                if (valueChangedEvent.newValue)
                    ApplyTickStateIfNeeded(_tickDataDisplayer.SelectedTick?.Snapshot);
            });

            _autoSelectTickToggle = root.Q<Toggle>("auto-select-control");

            // referring to play mode state
            EditorApplication.playModeStateChanged += AdjustControlsToPlayMode;

            _tickListDisplayer.TickListElement.onSelectionChange += selected =>
            {
                if (!_ignoreAutoApply)
                    ApplyTickStateIfNeeded((selected.FirstOrDefault() as TickEntryData)?.Snapshot);
            };
        }

        private void AdjustApplyStateButtonClickability() => _applyStateButton.SetEnabled(_pauseToggle.value && EditorApplication.isPlaying && !_autoApplyStateToggle.value);

        private void AdjustControlsToPlayMode(PlayModeStateChange state)
        {
            var startedPlayMode = state == PlayModeStateChange.EnteredPlayMode;

            _pauseToggle.SetEnabled(startedPlayMode);
            _pauseToggle.value = startedPlayMode;
        }

        internal void ApplyTickStateIfNeeded(ElympicsSnapshot snapshot, bool force = false)
        {
            if (!EditorApplication.isPlaying || snapshot == null || _replayClient == null || (!force && (!AutoApplyState || !Paused)))
                return;

            try
            {
                _replayClient.JumpToTick(snapshot.Tick);
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException($"Error applying tick {snapshot.Tick}.", e);
            }
        }

        public void LoadReplay(IReplayManipulatorClient replayClient, ReplayData data)
        {
            _currentTick = -1;
            _lastTick = data.Snapshots.Keys.Max();
            _tickListDisplayer.SetSnapshots(data.Snapshots.Values, data.InitData.Players, 1000 * ElympicsConfig.LoadCurrentElympicsGameConfig().TickDuration);
            _tickDataDisplayer.InitializeInputs(data.InitData.PlayerData.Select(player => player.IsBot).ToArray());

            _replayClient = replayClient;
            _replayClient.JumpToTick(data.Snapshots.Keys.Min());
            _replayClient.SetIsPlaying(false);
        }

        public void SetCurrentTick(long tick)
        {
            if (_replayClient == null || _currentTick == tick)
                return;

            _currentTick = tick;

            if (AutoSelectTick)
                _tickListDisplayer.SelectTick(tick);

            if (tick == _lastTick)
            {
                _replayClient.SetIsPlaying(false);
                _pauseToggle.value = true;
            }
        }
    }
}
