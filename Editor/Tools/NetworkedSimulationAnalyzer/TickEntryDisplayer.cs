using UnityEngine.UIElements;

namespace Elympics
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

        internal void SetTickEntryData(TickEntryData tickEntryData)
        {
            var timeUsage = tickEntryData.ExecutionTime / ServerAnalyzerUtils.ExpectedTime;

            _nrLabel.text = "#" + tickEntryData.Tick.ToString("000000");
            _executionTime.text = ServerAnalyzerUtils.FormatFloatMilliseconds(tickEntryData.ExecutionTime);
            _executionTime.style.color = ServerAnalyzerUtils.GetTimeUsageColor(timeUsage);

            _timeUsageOrb.style.backgroundColor = ServerAnalyzerUtils.GetTimeUsageColor(timeUsage);
        }
    }
}
