using System;
using System.Globalization;
using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    internal static class TimeUtil
    {
        public const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
        public static string DateTimeNowToString = DateTime.UtcNow.ToString(TimeFormat, CultureInfo.InvariantCulture);
    }

}

