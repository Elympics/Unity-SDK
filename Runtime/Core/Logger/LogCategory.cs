using System;
using UnityEngine;

namespace Elympics.Core.Logger
{
    internal enum LogCategory
    {
        Exception,
        Error,
        Warning,
        Info,
        Debug,
        Trace,
    }

    internal static class LogCategoryExtensions
    {
        public static LogLevel ToLogLevel(this LogCategory category) => category switch
        {
            LogCategory.Exception => LogLevel.Exception,
            LogCategory.Error => LogLevel.Error,
            LogCategory.Warning => LogLevel.Warning,
            LogCategory.Info or LogCategory.Debug or LogCategory.Trace => LogLevel.Log,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
        };

        public static LogType ToLogType(this LogCategory category) => category switch
        {
            LogCategory.Exception => LogType.Exception,
            LogCategory.Error => LogType.Error,
            LogCategory.Warning => LogType.Warning,
            LogCategory.Info or LogCategory.Debug or LogCategory.Trace => LogType.Log,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
        };
    }
}
