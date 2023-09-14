using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Elympics
{
    internal static class ErrorLogForwarder
    {
        private const int MaxStackFramesToLog = 5;
        private const int LogsSkipCallbackFrames = 3;
        private const string LogsAtUnityEngineNamespace = "  at " + nameof(UnityEngine);

        private static TextWriter errorOutput;
        private static readonly StringBuilder LoggerSb = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize() => Initialize(Console.Error);

        public static void Initialize(TextWriter textWriter)
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            errorOutput = textWriter;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type is not (LogType.Error or LogType.Assert or LogType.Exception))
                return;
            stackTrace ??= GetStackTraceForLog();
            errorOutput.WriteLine($"{condition}\n{stackTrace}\n\n");
        }

        private static string GetStackTraceForLog()
        {
            var stackTrace = SplitIntoLines(Environment.StackTrace);
            stackTrace = stackTrace.Skip(LogsSkipCallbackFrames).SkipWhile(x => x.StartsWith(LogsAtUnityEngineNamespace)).Take(MaxStackFramesToLog);
            _ = LoggerSb.Clear();
            foreach (var frame in stackTrace)
                _ = LoggerSb.AppendLine(frame);

            return LoggerSb.ToString();
        }

        private static IEnumerable<string> SplitIntoLines(string input)
        {
            using var reader = new StringReader(input);
            while (reader.ReadLine() is { } line)
                yield return line;
        }
    }
}
