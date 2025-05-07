using UnityEngine;

namespace Elympics.Editor.Replay
{
    public static class EditorReplayUtils
    {
        private const string MillisecondsSuffix = "ms";

        private static readonly float WarningThreshold = 0.8f;
        private static readonly float StrongWarningThreshold = 1.0f;

        public static readonly Color PositiveColor = new Color32(99, 222, 75, 255);
        public static readonly Color WarningColor = new Color32(212, 180, 36, 255);
        public static readonly Color StrongWarningColor = new Color32(228, 82, 82, 255);

        public static Color GetTimeUsageColor(float timeUsage)
        {
            if (timeUsage < WarningThreshold)
            {
                return PositiveColor;
            }
            else if (timeUsage < StrongWarningThreshold)
            {
                return WarningColor;
            }
            else
            {
                return StrongWarningColor;
            }
        }

        public static string FormatFloatMilliseconds(float value) => value.ToString("00.0", System.Globalization.CultureInfo.InvariantCulture) + MillisecondsSuffix;
    }
}
