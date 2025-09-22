using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UIElements;

namespace Elympics.Editor.Replay
{
    internal class TickDataDisplayer
    {
        private readonly VisualTreeAsset _inputDataTemplate;

        private readonly VisualElement _titleBar;
        private readonly VisualElement _contentsContainer;
        private readonly Label _nrLabel;
        private readonly Label _executionTime;
        private readonly Label _expectedTime;
        private readonly VisualElement _stateData;
        private readonly Foldout _inputsFoldout;
        private readonly Foldout _stateFoldout;

        private readonly StringBuilder _stringBuilder;
        private Label[] _inputsData;

        internal TickEntryData SelectedTick { get; private set; } = null;
        internal bool[] IsBots { get; private set; }

        internal TickDataDisplayer(VisualElement root, VisualTreeAsset inputDataTemplate)
        {
            _inputDataTemplate = inputDataTemplate;

            _titleBar = root.Q<VisualElement>("title-tick-bar");
            _contentsContainer = root.Q<VisualElement>("tick-data-view");
            _nrLabel = root.Q<Label>("title-tick-nr");
            _executionTime = root.Q<Label>("execution-time-value");
            _expectedTime = root.Q<Label>("expected-time-value");
            _stateData = root.Q<VisualElement>("behaviours-data");
            _inputsFoldout = root.Q<Foldout>("inputs-foldout");
            _stateFoldout = root.Q<Foldout>("state-foldout");

            _stringBuilder = new StringBuilder();
        }

        internal void SetData(TickEntryData tickEntryData, float expectedTime)
        {
            SelectedTick = tickEntryData;
            var visibility = SelectedTick != null ? Visibility.Visible : Visibility.Hidden;
            _titleBar.style.visibility = visibility;
            _contentsContainer.style.visibility = visibility;

            if (SelectedTick == null)
                return;

            // general data
            var timeUsage = tickEntryData.ExecutionTime / expectedTime;

            _nrLabel.text = tickEntryData.Tick.ToString("000000");
            _expectedTime.text = EditorReplayUtils.FormatFloatMilliseconds(expectedTime);
            _executionTime.text = EditorReplayUtils.FormatFloatMilliseconds(tickEntryData.ExecutionTime);
            _executionTime.style.color = EditorReplayUtils.GetTimeUsageColor(timeUsage);

            // inputs
            for (var i = 0; i < _inputsData.Length; i++)
            {
                _inputsData[i].text = tickEntryData.InputInfos[i].Message;
                _inputsData[i].style.color = tickEntryData.InputInfos[i].Color;
            }

            // synchronized state
            _stateFoldout.text = $"Synchronized state -> {CalculateTotalStateWeight(tickEntryData.Snapshot)} B";
            _stateData.Clear();
            foreach (var state in tickEntryData.SynchronizedState)
            {
                var behaviour = new Foldout
                {
                    text = $"{state.NetworkId} - {state.Name} -> {SizeOfNetworkData(tickEntryData.Snapshot.Data.Where(x => x.Key == state.NetworkId).First())} B"
                };
                _stateData.Add(behaviour);

                _ = _stringBuilder.Clear();

                var stateMetadata = state.StateMetadata;
                foreach (var (componentName, vars) in stateMetadata)
                {
                    if (stateMetadata.Count > 1)
                        _ = _stringBuilder.AppendLine($" {componentName}:");

                    foreach (var (elympicsVar, value) in vars)
                        _ = _stringBuilder.AppendLine($"    - {elympicsVar} = {value}");
                }

                var variables = new Label();
                variables.style.whiteSpace = WhiteSpace.Normal;
                variables.text = _stringBuilder.ToString();
                behaviour.Add(variables);
            }
        }

        internal void Deselect() => SetData(null, -1f);

        internal void InitializeInputs(bool[] isBots)
        {
            IsBots = isBots;
            _inputsData = new Label[isBots.Length];

            _inputsFoldout.Clear();

            for (var i = 0; i < isBots.Length; i++)
            {
                var newInputDisplay = _inputDataTemplate.CloneTree();
                _inputsData[i] = newInputDisplay.Q<Label>("value");
                var remoteName = newInputDisplay.Q<Label>("text");
                remoteName.text = !isBots[i] ? $"Remote {i}:" : $"Remote {i} (bot):";
                remoteName.style.minWidth = 100;
                _inputsFoldout.Add(newInputDisplay);
            }
        }

        internal int CalculateTotalStateWeight(ElympicsSnapshotWithMetadata snapshot)
        {
            // initialize with Tick and TickStartUtc length
            var byteSum = sizeof(long) + ElympicsSnapshot.DateTimeWeight;

            foreach (var item in snapshot.Data)
            {
                byteSum += SizeOfNetworkData(item);
            }

            foreach (var item in snapshot.Factory.Parts)
            {
                byteSum += SizeOfNetworkData(item);
            }

            return byteSum;
        }

        private int SizeOfNetworkData(KeyValuePair<int, byte[]> item) => sizeof(int) + item.Value.Length;
    }
}
