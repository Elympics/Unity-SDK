using System;
using System.Globalization;
using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    internal static class TimeUtil
    {
        public static string DateTimeNowToString => DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
    }

}

