#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elympics.Core.Logger.Builder;
using Object = UnityEngine.Object;

namespace Elympics.Core.Logger
{
    internal static class ElympicsLogger
    {
        private static readonly Guid SessionId = Guid.NewGuid();
        private static readonly List<ILogOutlet> RegisteredOutlets = new()
        {
            new PlainLogOutlet(),
            new JsonLogOutlet(),
        };

        public static ApplicationState ApplicationState { get; } = new(ElympicsVersionRetriever.GetVersionStringFromAssembly());
        public static ElympicsLoggerConfig Config { get; set; } = new();

        private static void Log(LogCategory category, string message, ElympicsLoggerConfig config, string? stacktrace = null, Object? unityContext = null)
        {
            var time = DateTime.Now;
            if (stacktrace is null && (category is LogCategory.Exception || config.StacktraceForEverything))
                stacktrace = new StackTrace(2, true).ToString();
            foreach (var outlet in RegisteredOutlets)
                outlet.Log(category, time, message, stacktrace, ApplicationState, config);
        }

        [Conditional("ELYMPICS_DEBUG")] public static void LogDebug(string message, Object? context = null) => Log(LogCategory.Debug, message, Config, unityContext: context);
        [Conditional("ELYMPICS_DEBUG")] public static void LogDebug(this ElympicsLoggerConfig config, string message, Object? context = null) => Log(LogCategory.Debug, message, config, unityContext: context);


        [Conditional("ELYMPICS_TRACE")] public static void LogTrace(string message, Object? context = null) => Log(LogCategory.Trace, message, Config, unityContext: context);
        [Conditional("ELYMPICS_TRACE")] public static void LogTrace(this ElympicsLoggerConfig config, string message, Object? context = null) => Log(LogCategory.Trace, message, config, unityContext: context);

        public static void LogInfo(string message, Object? context = null) => Log(LogCategory.Info, message, Config, unityContext: context);
        public static void LogInfo(this ElympicsLoggerConfig config, string message, Object? context = null) => Log(LogCategory.Info, message, config, unityContext: context);

        public static void LogWarning(string message, Object? context = null) => Log(LogCategory.Warning, message, Config, unityContext: context);
        public static void LogWarning(this ElympicsLoggerConfig config, string message, Object? context = null) => Log(LogCategory.Warning, message, config, unityContext: context);

        public static void LogError(string message, Object? context = null) => Log(LogCategory.Error, message, Config, unityContext: context);
        public static void LogError(this ElympicsLoggerConfig config, string message, Object? context = null) => Log(LogCategory.Error, message, config, unityContext: context);

        // TODO: inner exceptions ~dsygocki 2026-05-26
        public static void LogException(Exception exception, Object? context = null) => Log(LogCategory.Exception, exception.Message, Config, exception.StackTrace, context);
        public static void LogException(this ElympicsLoggerConfig config, Exception exception, Object? context = null) => Log(LogCategory.Exception, exception.Message, config, exception.StackTrace, context);

        // TODO: inner exceptions ~dsygocki 2026-05-26
        public static Exception LogExceptionAndReturn(Exception exception, Object? context = null)
        {
            Log(LogCategory.Exception, exception.Message, Config, exception.StackTrace, context);
            return exception;
        }
        public static Exception LogExceptionAndReturn(this ElympicsLoggerConfig config, Exception exception, Object? context = null)
        {
            Log(LogCategory.Exception, exception.Message, config, exception.StackTrace, context);
            return exception;
        }
    }
}
