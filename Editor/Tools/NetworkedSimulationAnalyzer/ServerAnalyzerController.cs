#if UNITY_2020_2_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Elympics
{
    internal class ServerAnalyzerController
    {
        private readonly ToolbarToggle _pauseToggle;
        private readonly Button _saveButton;
        private readonly Button _loadButton;
        private readonly Label _helpLink;
        private readonly Button _applyStateButton;
        private readonly Toggle _autoApplyStateToggle;

        private TickListDisplayer _tickListDisplayer;
        private TickDataDisplayer _tickDataDisplayer;

        private Action<ElympicsSnapshot> _snapshotApplier;
        private long _currentlyAppliedTick = -1; // negative indicates latest tick
        private TickEntryData _tickBeforeLoad = null;
        private bool AutoApplyState => _autoApplyStateToggle.value;
        internal bool Paused => _pauseToggle.value;

        internal ServerAnalyzerController(VisualElement root)
        {
            // pause toggle setup
            _pauseToggle = root.Q<ToolbarToggle>("pause-control");
            // because used element template contains useless element which have to be disabled - it takes up space!
            _pauseToggle.Q<VisualElement>(classes: "unity-toggle__input").style.display = DisplayStyle.None; 
            _pauseToggle.SetEnabled(EditorApplication.isPlaying);
            _pauseToggle.RegisterValueChangedCallback(valueChangedEvent =>
            {
                _saveButton.SetEnabled(valueChangedEvent.newValue);
                _loadButton.SetEnabled(valueChangedEvent.newValue);
                AdjustApplyStateButtonClickability();

                if (!valueChangedEvent.newValue)
                {
                    ApplyLatestTickIfNeeded();
                }
            });

            _saveButton = root.Q<Button>("save-control");
            _saveButton.SetEnabled(false);
            _saveButton.clicked += () =>
            {
                ReplayFileManager.SaveServerReplay(_tickListDisplayer, _tickDataDisplayer);
            };

            _loadButton = root.Q<Button>("load-control");
            _loadButton.SetEnabled(!EditorApplication.isPlaying);
            _loadButton.clicked += () =>
            {
                var tickBeforeLoad = _tickListDisplayer.LatestTick;

                ReplayFileManager.LoadServerReplay(_tickListDisplayer, _tickDataDisplayer);

                ApplyLatestTickIfNeeded(true);
                _tickDataDisplayer.Deselect();
                _tickBeforeLoad = tickBeforeLoad;

                if (EditorApplication.isPlaying)
                {
                    _pauseToggle.SetEnabled(false);
                }
            };

            _helpLink = root.Q<Label>("help-control");
            _helpLink.RegisterCallback<MouseDownEvent> (e => 
            {
                Application.OpenURL("https://docs.elympics.cc/testing-troubleshooting/networked-simulation-analyzer/");
            });

            // apply state button setup
            _applyStateButton = root.Q<Button>("apply-state-button");
            _applyStateButton.SetEnabled(false);
            _applyStateButton.clicked += () =>
            {
                ApplyTickStateIfNeeded(_tickDataDisplayer.SelectedTick?.Snapshot, true);
            };

            // auto apply state toggle setup
            _autoApplyStateToggle = root.Q<Toggle>("auto-apply-control");
            _autoApplyStateToggle.RegisterValueChangedCallback(valueChangedEvent =>
            {
                AdjustApplyStateButtonClickability();

                if (valueChangedEvent.newValue)
                {
                    ApplyTickStateIfNeeded(_tickDataDisplayer.SelectedTick?.Snapshot);
                }
            });

            // referring to play mode state
            EditorApplication.playModeStateChanged += AdjustControlsToPlayMode;
        }

        private void AdjustApplyStateButtonClickability()
        {
            _applyStateButton.SetEnabled(_pauseToggle.value && EditorApplication.isPlaying && !_autoApplyStateToggle.value);
        }

        internal void OnExit()
        {
            // if has loaded before, bring back state from before it, else go to newest state
            if (_tickBeforeLoad != null)
                ApplyLatestTickIfNeeded(true, true);
            else
                ApplyLatestTickIfNeeded();

            _snapshotApplier = null;
        }

        internal void SetSnapshotApplier(Action<ElympicsSnapshot> snapshotApplier)
        {
            _snapshotApplier = snapshotApplier;
        }

        internal void Initialize(TickListDisplayer tickListDisplayer, TickDataDisplayer tickDataDisplayer)
        {
            _tickListDisplayer = tickListDisplayer;
            _tickDataDisplayer = tickDataDisplayer;
        }

        private void AdjustControlsToPlayMode(PlayModeStateChange state)
        {
            bool startedPlayMode = state == PlayModeStateChange.EnteredPlayMode;

            _pauseToggle.SetEnabled(startedPlayMode);
            _pauseToggle.value = false;

            _loadButton.SetEnabled(!startedPlayMode);
            _saveButton.SetEnabled(!startedPlayMode);
        }

        internal void ApplyTickStateIfNeeded(ElympicsSnapshot snapshot, bool force = false)
        {
            if (!EditorApplication.isPlaying || snapshot == null || (!force && (!AutoApplyState || !Paused)))
                return;

            Debug.Log($"Going to snapshot tick {snapshot.Tick}");
            try
            {
                _snapshotApplier(snapshot);
            }
            catch (Exception e)
            {
                Debug.LogException(new Exception($"Error applying snapshot tick {snapshot.Tick}", e));
                return;
            }

            _currentlyAppliedTick = snapshot.Tick;

            var oldPhysicsAutoSimulation = Physics.autoSimulation;
            var oldPhysics2DAutoSimulation = Physics2D.simulationMode;
            Physics.autoSimulation = false;
            Physics2D.simulationMode = SimulationMode2D.Script;

            Physics.Simulate(float.Epsilon);
            Physics2D.Simulate(float.Epsilon);

            Physics.autoSimulation = oldPhysicsAutoSimulation;
            Physics2D.simulationMode = oldPhysics2DAutoSimulation;
        }

        internal void ApplyLatestTickIfNeeded(bool force = false, bool reloadedPreLoadState = false)
        {
            if (!EditorApplication.isPlaying || _snapshotApplier == null)
                return;

            if (_currentlyAppliedTick >= 0 || force)
            {
                Debug.LogWarning($"Forcefully applied latest recorded state{(reloadedPreLoadState ? " from before loading the replay file" : string.Empty)}");
                ApplyTickStateIfNeeded(reloadedPreLoadState ? _tickBeforeLoad.Snapshot : _tickListDisplayer.LatestTick.Snapshot, true);
                _currentlyAppliedTick = -1;
            }
        }
    }
}
#endif
