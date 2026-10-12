using System;
using System.Runtime.CompilerServices;

namespace Elympics.Editor.Weaving.Components
{
    internal interface ILogger
    {
        void LogInfo(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);
        void LogWarning(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);
        void LogError(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);
        void LogException(Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);
    }
}
