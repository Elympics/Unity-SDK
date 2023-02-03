#if UNITY_2020_2_OR_NEWER
using UnityEngine;

namespace Elympics
{
    public static class ServerAnalyzerUtils
    {
        private const string MillisecondsSuffix = "ms";

        private readonly static float WarningThreshold = 0.8f;
        private readonly static float StrongWarningThreshold = 1.0f;

        public readonly static Color PositiveColor = new Color32(99, 222, 75, 255);
        public readonly static Color WarningColor = new Color32(212, 180, 36, 255);
        public readonly static Color StrongWarningColor = new Color32(228, 82, 82, 255);

        // a bit dirty but... sooo convenient!
        public static float ExpectedTime { get; set; }

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

        public static string FormatFloatMilliseconds(float value)
        {
            return value.ToString("00.0", System.Globalization.CultureInfo.InvariantCulture) + MillisecondsSuffix;
        }
    }
}
#endif
