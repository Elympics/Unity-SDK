using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Elympics
{
    internal static class ElympicsLogger
    {
        private const string ElympicsPrefix = "[Elympics] ";

        private static Stopwatch timer;
        private static readonly StringBuilder StringBuilder = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
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
                    .Append(ElympicsPrefix)
#if !UNITY_EDITOR
                    .Append(FormatTimeSpan(timer.Elapsed))
#endif
                    .Append(message)
                    .ToString();
            }
        }

        public static void Log(string message) =>
            Debug.Log(PrependWithDetails(message));
        public static void Log(string message, Object context) =>
            Debug.Log(PrependWithDetails(message), context);
        public static void LogFormat(string format, params object[] args) =>
            Debug.LogFormat(PrependWithDetails(format), args);
        public static void LogFormat(Object context, string format, params object[] args) =>
            Debug.LogFormat(context, PrependWithDetails(format), args);

        public static void LogWarning(string message) =>
            Debug.LogWarning(PrependWithDetails(message));
        public static void LogWarning(string message, Object context) =>
            Debug.LogWarning(PrependWithDetails(message), context);
        public static void LogWarningFormat(string format, params object[] args) =>
            Debug.LogWarningFormat(PrependWithDetails(format), args);
        public static void LogWarningFormat(Object context, string format, params object[] args) =>
            Debug.LogWarningFormat(context, PrependWithDetails(format), args);

        public static void LogError(string message) =>
            Debug.LogError(PrependWithDetails(message));
        public static void LogError(string message, Object context) =>
            Debug.LogError(PrependWithDetails(message), context);
        public static void LogErrorFormat(string format, params object[] args) =>
            Debug.LogErrorFormat(PrependWithDetails(format), args);
        public static void LogErrorFormat(Object context, string format, params object[] args) =>
            Debug.LogErrorFormat(context, PrependWithDetails(format), args);

        public static Exception LogException(string message, Object context = null)
        {
            var exception = new ElympicsException(message);
            Debug.LogException(exception, context);
            return exception;
        }
        public static Exception LogException(string message, Exception inner, Object context = null)
        {
            var exception = new ElympicsException(message, inner);
            Debug.LogException(exception, context);
            return exception;
        }
        public static Exception LogException(Exception exception, Object context = null)
        {
            var wrappedException = exception is not ElympicsException
                ? new ElympicsException("Caught exception", exception)
                : exception;
            Debug.LogException(wrappedException, context);
            return exception;
        }
    }
}
