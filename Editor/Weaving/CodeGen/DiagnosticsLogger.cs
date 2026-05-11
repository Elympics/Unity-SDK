#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elympics.Editor.Weaving.Components;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace Elympics.Editor.CodeGen
{
    internal class DiagnosticsLogger : ILogger
    {
        private readonly IList<DiagnosticMessage> _messageList;
        private readonly string _prefix;

        public DiagnosticsLogger(IList<DiagnosticMessage> messageList, string? prefix) => (_messageList, _prefix) = (messageList, prefix ?? "");

        private void Log(DiagnosticType type, string message, string filePath, int lineNumber) => _messageList.Add(new DiagnosticMessage
        {
            DiagnosticType = type,
            File = filePath,
            Line = lineNumber,
            MessageData = _prefix + message,
        });

        public void LogInfo(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) =>
            Log(default, message, filePath, lineNumber);

        public void LogWarning(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) =>
            Log(DiagnosticType.Warning, message, filePath, lineNumber);

        public void LogError(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) =>
            Log(DiagnosticType.Error, message, filePath, lineNumber);

        public void LogException(Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            var message = exception.Message;
            var stackTrace = exception.StackTrace;
            Log(DiagnosticType.Error,
                string.IsNullOrEmpty(stackTrace) ? message : message + "|" + stackTrace?.Replace('\n', '|'),
                filePath,
                lineNumber);
        }
    }
}
