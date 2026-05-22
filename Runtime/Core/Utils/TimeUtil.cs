using System;
using System.Globalization;
using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    internal static class TimeUtil
    {
        public static string DateTimeNowAsString => DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        public static string DateTimeToString(DateTime dateTime) => dateTime.ToString("O", CultureInfo.InvariantCulture);
        public static DateTime DateTimeFromString(string dateTime) => DateTime.ParseExact(dateTime, "O", CultureInfo.InvariantCulture);
    }
}
