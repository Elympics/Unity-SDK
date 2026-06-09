#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elympics.Core.Logger.Builder;
using Object = UnityEngine.Object;

namespace Elympics.Core.Logger
{
    internal static class ElympicsLogger
    {
        private static readonly List<ILogOutlet> RegisteredOutlets = new()
        {
            new PlainLogOutlet(),
            new JsonLogOutlet(),
        };

        public static ApplicationState ApplicationState { get; } = new(ElympicsVersionRetriever.GetVersionStringFromAssembly());
        public static LoggerConfig Config { get; set; } = new();

        private static void Log(LogCategory category, string message, LoggerConfig config, string? stacktrace = null, Object? unityContext = null)
        {
            var time = DateTime.Now;
            if (stacktrace is null && (category is LogCategory.Exception || config.StacktraceForEverything))
                stacktrace = new StackTrace(2, true).ToString();
            foreach (var outlet in RegisteredOutlets)
                outlet.Log(category, time, message, stacktrace, ApplicationState, config);
        }

        [Conditional("ELYMPICS_DEBUG")] public static void LogDebug(string message) => Log(LogCategory.Debug, message, Config);
        [Conditional("ELYMPICS_DEBUG")] public static void LogDebug(this LoggerConfig config, string message) => Log(LogCategory.Debug, message, config);


        [Conditional("ELYMPICS_TRACE")] public static void LogTrace(string message) => Log(LogCategory.Trace, message, Config);
        [Conditional("ELYMPICS_TRACE")] public static void LogTrace(this LoggerConfig config, string message) => Log(LogCategory.Trace, message, config);

        public static void LogInfo(string message) => Log(LogCategory.Info, message, Config);
        public static void LogInfo(this LoggerConfig config, string message) => Log(LogCategory.Info, message, config);

        public static void LogWarning(string message) => Log(LogCategory.Warning, message, Config);
        public static void LogWarning(this LoggerConfig config, string message) => Log(LogCategory.Warning, message, config);

        public static void LogError(string message) => Log(LogCategory.Error, message, Config);
        public static void LogError(this LoggerConfig config, string message) => Log(LogCategory.Error, message, config);

        // TODO: inner exceptions ~dsygocki 2026-05-26
        public static void LogException(Exception exception) => Log(LogCategory.Exception, exception.Message, Config, exception.StackTrace);
        public static void LogException(this LoggerConfig config, Exception exception) => Log(LogCategory.Exception, exception.Message, config, exception.StackTrace);

        // TODO: inner exceptions ~dsygocki 2026-05-26
        public static Exception LogExceptionAndReturn(Exception exception)
        {
            Log(LogCategory.Exception, exception.Message, Config, exception.StackTrace);
            return exception;
        }
        public static Exception LogExceptionAndReturn(this LoggerConfig config, Exception exception)
        {
            Log(LogCategory.Exception, exception.Message, config, exception.StackTrace);
            return exception;
        }

        public static LoggerConfig WithMonitoringEnabled() => Config.WithMonitoringEnabled();
        public static LoggerConfig WithStacktraceForEverything() => Config.WithStacktraceForEverything();
        public static LoggerConfig WithUnityContext(Object context) => Config.WithUnityContext(context);
        public static LoggerConfig WithMethodName([CallerMemberName] string methodName = "") => Config.WithMehodName(methodName);
        public static LoggerConfig WithClassName(string className) => Config.WithClassName(className);
        public static LoggerConfig WithServiceName(string serviceName) => Config.WithServiceName(serviceName);
        public static LoggerConfig WithElympicsSdkService() => Config.WithElympicsSdkService();
        public static LoggerConfig WithElympicsGameService() => Config.WithElympicsGameService();
        public static LoggerConfig WithPlayPadSdkService() => Config.WithPlayPadSdkService();
    }
}
