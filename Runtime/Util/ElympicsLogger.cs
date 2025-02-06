using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Elympics.ElympicsSystems.Internal;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Elympics
{
    internal static class ElympicsLogger
    {
        internal const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
        internal static ElympicsLoggerContext? CurrentContext;
        internal static Guid SessionId;
        private const string AppPrefixFormat = "[{0}] ";
        private const string DefaultApp = "ElympicsSdk";

        private static Stopwatch timer;
        private static readonly StringBuilder StringBuilder = new();
        private static readonly List<IElympicsLoggerClient> Clients = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            SessionId = new Guid();
            timer = new Stopwatch();
            timer.Start();
        }

#if !UNITY_EDITOR
        private static string FormatTimeSpan(TimeSpan ts) =>
            $"[{ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}] ";
#endif

        public static string PrependWithDetails(string message)
        {
            lock (StringBuilder)
            {
                return StringBuilder.Clear()
#if !UNITY_EDITOR
#endif
                    .Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ") + " ")
                    .Append(string.Format(AppPrefixFormat, DefaultApp)).Append(message).ToString();
            }
        }

        public static void RegisterLoggerClient(IElympicsLoggerClient client) => Clients.Add(client);

        public static void UnregisterLoggerClient(IElympicsLoggerClient client) => Clients.Remove(client);

        private static void InformClients(ElympicsLoggerContext context, LogLevel logLevel) => Clients.ForEach(x => x.LogCaptured(context, logLevel));

        #region Logs

        public static void Log(string message) => Debug.Log(PrependWithDetails(message));
        public static void Log(string message, Object context) => Debug.Log(PrependWithDetails(message), context);
        public static void LogFormat(string format, params object[] args) => Debug.LogFormat(PrependWithDetails(format), args);
        public static void LogFormat(Object context, string format, params object[] args) => Debug.LogFormat(context, PrependWithDetails(format), args);
        public static void Log(ElympicsLoggerContext context)
        {
            Debug.Log(context.ToString());
            InformClients(context, LogLevel.Log);
        }

        #endregion

        #region Warnings

        public static void LogWarning(string message) => Debug.LogWarning(PrependWithDetails(message));
        public static void LogWarning(string message, Object context) => Debug.LogWarning(PrependWithDetails(message), context);
        public static void LogWarningFormat(string format, params object[] args) => Debug.LogWarningFormat(PrependWithDetails(format), args);
        public static void LogWarningFormat(Object context, string format, params object[] args) => Debug.LogWarningFormat(context, PrependWithDetails(format), args);
        public static void LogWarning(ElympicsLoggerContext context)
        {
            Debug.LogWarning(context);
            InformClients(context, LogLevel.Warning);
        }

        #endregion

        #region Errors

        public static void LogError(string message)
        {
            Debug.LogError(PrependWithDetails(message));
        }
        public static void LogError(string message, Object context)
        {
            Debug.LogError(PrependWithDetails(message), context);
        }
        public static void LogErrorFormat(string format, params object[] args)
        {
            Debug.LogErrorFormat(PrependWithDetails(format), args);
        }
        public static void LogErrorFormat(Object context, string format, params object[] args)
        {
            Debug.LogErrorFormat(context, PrependWithDetails(format), args);
        }
        public static void LogError(ElympicsLoggerContext context)
        {
            Debug.LogError(context);
            InformClients(context, LogLevel.Error);
        }

        #endregion

        #region Exceptions

        public static Exception LogException(string message, Object context = null)
        {
            var exception = new ElympicsException(PrependWithDetails(message));
            Debug.LogException(exception, context);
            return exception;
        }
        public static Exception LogException(string message, Exception inner, Object context = null)
        {
            var exception = new ElympicsException(PrependWithDetails(message), inner);
            Debug.LogException(exception, context);
            return exception;
        }
        public static Exception LogException(Exception exception, Object context = null)
        {
            var wrappedException = exception is not ElympicsException ? new ElympicsException("Caught exception", exception) : exception;
            Debug.LogException(wrappedException, context);
            return exception;
        }

        public static Exception CaptureAndThrow(Exception exception, ElympicsLoggerContext loggerContext)
        {
            var wrappedException = exception is not ElympicsException ? new ElympicsException(loggerContext.ToString(), exception) : exception;
            InformClients(loggerContext, LogLevel.Exception);
            return wrappedException;
        }

        public static void LogException(Exception exception, ElympicsLoggerContext loggerContext, Object context = null)
        {
            Debug.LogException(exception, context);
            InformClients(loggerContext, LogLevel.Exception);
        }

        #endregion
    }
}
