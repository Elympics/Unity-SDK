using System;
using System.Diagnostics;
using System.Text;
using Elympics.AssemblyCommunicator;
using Elympics.ElympicsSystems.Internal;
using Elympics.Events;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Elympics
{
    internal static class ElympicsLogger
    {
        internal static ElympicsLoggerContext? CurrentContext;
        internal static Guid SessionId;

        private const string LogStringFormat = "[{0,-28}] [{1}] {2}";
        private const string AppPrefixFormat = "[{0}] ";
        private const string DefaultApp = "ElympicsSdk";

        private static Stopwatch timer;
        private static readonly StringBuilder StringBuilder = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            SessionId = Guid.NewGuid();
            timer = new Stopwatch();
            timer.Start();
        }

        private static string PrependWithDetails(string message)
        {
            lock (StringBuilder)
            {
                return StringBuilder.Clear()
#if !UNITY_EDITOR
                    .Append(TimeUtil.DateTimeNowToString + " ")
#endif
                    .Append(string.Format(AppPrefixFormat, DefaultApp)).Append(message).ToString();
            }
        }

        private static string PrependWithDetails(string message, string time, ElympicsLoggerContext context)
        {
            lock (StringBuilder)
                return StringBuilder.Clear().AppendLine(string.Format(LogStringFormat, time, context.App, message)).AppendLine(context.ToString()).ToString();
        }

        private static void InformClients(string message, string time, ElympicsLoggerContext context, LogLevel logLevel) => CrossAssemblyEventBroadcaster.RaiseEvent(new ElympicsLogEvent() { Message = message, Time = time, Context = context, LogLevel = logLevel });

        #region Logs

        public static void Log(string message) => Debug.Log(PrependWithDetails(message));
        public static void Log(string message, Object context) => Debug.Log(PrependWithDetails(message), context);
        public static void LogFormat(string format, params object[] args) => Debug.LogFormat(PrependWithDetails(format), args);
        public static void LogFormat(Object context, string format, params object[] args) => Debug.LogFormat(context, PrependWithDetails(format), args);
        public static void Log(string message, string time, ElympicsLoggerContext context)
        {
            Debug.Log(PrependWithDetails(message, time, context));
            InformClients(message, time, context, LogLevel.Log);
        }

        #endregion

        #region Warnings

        public static void LogWarning(string message) => Debug.LogWarning(PrependWithDetails(message));
        public static void LogWarning(string message, Object context) => Debug.LogWarning(PrependWithDetails(message), context);
        public static void LogWarningFormat(string format, params object[] args) => Debug.LogWarningFormat(PrependWithDetails(format), args);
        public static void LogWarningFormat(Object context, string format, params object[] args) => Debug.LogWarningFormat(context, PrependWithDetails(format), args);
        public static void LogWarning(string message, string time, ElympicsLoggerContext context)
        {
            Debug.LogWarning(PrependWithDetails(message, time, context));
            InformClients(message, time, context, LogLevel.Warning);
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
        public static void LogError(string message, string time, ElympicsLoggerContext context)
        {
            Debug.LogError(PrependWithDetails(message, time, context));
            InformClients(message, time, context, LogLevel.Error);
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

        public static Exception CaptureAndThrow(Exception exception, string time, ElympicsLoggerContext loggerContext)
        {
            var wrappedException = exception is not ElympicsException ? new ElympicsException(exception.Message, exception) : exception;
            InformClients(exception.Message, time, loggerContext, LogLevel.Exception);
            return wrappedException;
        }

        public static void LogException(Exception exception, string time, ElympicsLoggerContext loggerContext, Object context = null)
        {
            Debug.LogException(exception, context);
            InformClients(exception.Message, time, loggerContext, LogLevel.Exception);
        }

        #endregion
    }
}
