#nullable enable

using System;
using System.Diagnostics;
using System.Text;
using Elympics.AssemblyCommunicator;
using Elympics.Events;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Elympics.Core.Logger
{
    internal static class ElympicsLogger
    {
        private static readonly Guid SessionId = Guid.NewGuid();
        internal static ApplicationState CurrentContext = new(SessionId);

        private const string LogStringFormat = "[{0,-28}] [{1}] {2}";
        private const string AppPrefixFormat = "[{0}] ";
        private const string DefaultApp = "ElympicsSdk";

        private static readonly StringBuilder StringBuilder = new();

        private static string PrependWithDetails(string message)
        {
            lock (StringBuilder)
                return StringBuilder.Clear()
#if !UNITY_EDITOR
                    .Append(TimeUtil.DateTimeNowAsString + " ")
#endif
                    .Append(string.Format(AppPrefixFormat, DefaultApp)).Append(message).ToString();
        }

        private static string PrependWithDetails(string message, string time, ApplicationState context)
        {
            lock (StringBuilder)
                return StringBuilder.Clear().AppendFormat(LogStringFormat, time, context.App, message).AppendLine().AppendLine(context.ToString()).ToString();
        }

        private static void InformClients(string message, string time, ApplicationState context, LogLevel logLevel) =>
            CrossAssemblyEventBroadcaster.RaiseEvent(new ElympicsLogEvent
            {
                Message = message,
                Time = time,
                Context = context,
                LogLevel = logLevel,
            });

        #region Debug-only logs

        [Conditional("ELYMPICS_DEBUG")] public static void LogDebug(string message, Object? context = null) => Debug.Log(PrependWithDetails(message), context);

        [Conditional("ELYMPICS_DEBUG")]
        public static void LogDebug(this ApplicationState context, string message)
        {
            var time = TimeUtil.DateTimeNowAsString;
            Debug.Log(PrependWithDetails(message, time, context));
            InformClients(message, time, context, LogLevel.Log);
        }

        #endregion

        #region Trace-level debug logs

        [Conditional("ELYMPICS_TRACE")] public static void LogTrace(string message, Object? context = null) => Debug.Log(PrependWithDetails(message), context);

        [Conditional("ELYMPICS_TRACE")]
        public static void LogTrace(this ApplicationState state, string message, Object? context = null)
        {
            var time = TimeUtil.DateTimeNowAsString;
            Debug.Log(PrependWithDetails(message, time, state), context);
            InformClients(message, time, state, LogLevel.Log);
        }

        #endregion

        #region Logs

        public static void LogInfo(string message, Object? context = null) => Debug.Log(PrependWithDetails(message), context);

        public static void LogInfo(this ApplicationState state, string message, Object? context = null)
        {
            var time = TimeUtil.DateTimeNowAsString;
            Debug.Log(PrependWithDetails(message, time, state), context);
            InformClients(message, time, state, LogLevel.Log);
        }

        #endregion

        #region Warnings

        public static void LogWarning(string message, Object? context = null) => Debug.LogWarning(PrependWithDetails(message), context);

        public static void LogWarning(this ApplicationState state, string message, Object? context = null)
        {
            var time = TimeUtil.DateTimeNowAsString;
            Debug.LogWarning(PrependWithDetails(message, time, state), context);
            InformClients(message, time, state, LogLevel.Warning);
        }

        #endregion

        #region Errors

        public static void LogError(string message, Object? context = null) => Debug.LogError(PrependWithDetails(message), context);

        public static void LogError(this ApplicationState state, string message, Object? context = null)
        {
            var time = TimeUtil.DateTimeNowAsString;
            Debug.LogError(PrependWithDetails(message, time, state), context);
            InformClients(message, time, state, LogLevel.Error);
        }

        #endregion

        #region Exceptions

        public static Exception LogExceptionAndReturn(Exception exception, Object? context = null)
        {
            var wrappedException = exception is not ElympicsException ? new ElympicsException("Caught exception", exception) : exception;
            Debug.LogException(wrappedException, context);
            return exception;
        }

        public static void LogException(Exception exception, Object? context = null)
        {
            var wrappedException = exception is not ElympicsException ? new ElympicsException("Caught exception", exception) : exception;
            Debug.LogException(wrappedException, context);
        }

        public static Exception LogExceptionAndReturn(this ApplicationState state, Exception exception, Object? context = null)
        {
            var time = TimeUtil.DateTimeNowAsString;
            var wrappedException = exception is not ElympicsException ? new ElympicsException(exception.Message, exception) : exception;
            Debug.LogException(exception, context);
            InformClients(exception.Message, time, state, LogLevel.Exception);
            return wrappedException;
        }

        public static void LogException(this ApplicationState state, Exception exception, Object? context = null)
        {
            var time = TimeUtil.DateTimeNowAsString;
            Debug.LogException(exception, context);
            InformClients(exception.Message, time, state, LogLevel.Exception);
        }

        #endregion
    }
}
