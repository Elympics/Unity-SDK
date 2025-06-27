using UnityEngine.UIElements;

namespace Elympics.Editor.Replay
{
    internal class TickEntryDisplayer
    {
        private Label _nrLabel;
        private Label _executionTime;
        private VisualElement _timeUsageOrb;

        internal void InitVisualElement(VisualElement visualElement)
        {
            _nrLabel = visualElement.Q<Label>("nr");
            _executionTime = visualElement.Q<Label>("execution-time");
            _timeUsageOrb = visualElement.Q<VisualElement>("time-usage-orb");
        }

        internal void SetTickEntryData(TickEntryData tickEntryData, float expectedTime)
        {
            var timeUsage = tickEntryData.ExecutionTime / expectedTime;

            _nrLabel.text = "#" + tickEntryData.Tick.ToString("000000");
            _executionTime.text = EditorReplayUtils.FormatFloatMilliseconds(tickEntryData.ExecutionTime);
            _executionTime.style.color = EditorReplayUtils.GetTimeUsageColor(timeUsage);

            _timeUsageOrb.style.backgroundColor = EditorReplayUtils.GetTimeUsageColor(timeUsage);
        }
    }
}
