#nullable enable
using System;
using System.Text;
using Elympics.AssemblyCommunicator;
using Elympics.Core.Logger.State;
using Elympics.Events;

namespace Elympics.Core.Logger.Builder
{
    internal class PlaypadBreadcrumbOutlet : ILogOutlet
    {
        private static readonly object StringBuilderLock = new();
        private readonly StringBuilder _stringBuilder;
        private readonly JsonStateVisitor _stateVisitor;
        private readonly OutputLogger _outputLogger;

        public delegate void OutputLogger(LogLevel logLevel, string isoTimestamp, string messageJson);

        public PlaypadBreadcrumbOutlet(OutputLogger? outputLogger = null)
        {
            _stringBuilder = new StringBuilder();
            _stateVisitor = new JsonStateVisitor(_stringBuilder);
            _outputLogger = outputLogger ?? BroadcastLogEvent;
        }

        public void Log(
            LogCategory category,
            DateTime time,
            string message,
            string? stacktrace,
            ApplicationState state,
            LoggerConfig config)
        {
            if (!config.MonitoringEnabled)
                return;

            var logLevel = category.ToLogLevel();
            var isoTimestamp = TimeUtil.DateTimeToString(time);
            string finalMessage;
            lock (StringBuilderLock)
            {
                _ = _stringBuilder.Clear()
                    .Append('{');
                AppendProperty(_stringBuilder, "level", (int)logLevel, isFirst: true);
                AppendProperty(_stringBuilder, "message", message);
                if (stacktrace is not null)
                    AppendProperty(_stringBuilder, nameof(stacktrace), stacktrace);
                _ = _stringBuilder.Append(",\"data\":{");
                AppendProperty(_stringBuilder, "time", isoTimestamp, isFirst: true);
                _ = config.Context.Visit(_stateVisitor);
                _ = state.Visit(_stateVisitor);
                finalMessage = _stringBuilder.Append('}')
                    .Append('}')
                    .ToString();
            }
            _outputLogger.Invoke(logLevel, isoTimestamp, finalMessage);
        }

        private static void BroadcastLogEvent(LogLevel logLevel, string isoTimestamp, string messageJson)
        {
            CrossAssemblyEventBroadcaster.RaiseEvent(new ElympicsLogEvent
            {
                LogLevel = logLevel,
                Time = isoTimestamp,
                Json = messageJson,
            });
        }

        private class JsonStateVisitor : IStateVisitor
        {
            private readonly StringBuilder _stringBuilder;

            public JsonStateVisitor(StringBuilder stringBuilder) => _stringBuilder = stringBuilder;

            public void ProcessSubstate(string name, string? legacyName = null) { }

            public void ProcessProperty(string name, string value, string? legacyName = null)
            {
                AppendProperty(_stringBuilder, name, value);
                if (legacyName is not null)
                    AppendProperty(_stringBuilder, legacyName, value);
            }
        }

        private static void AppendProperty(StringBuilder sb, string key, string? value, bool isFirst = false)
        {
            if (!isFirst)
                _ = sb.Append(',');
            AppendEscaped(sb, key);
            _ = sb.Append(':');
            AppendEscaped(sb, value);
        }

        private static void AppendProperty(StringBuilder sb, string key, int value, bool isFirst = false)
        {
            if (!isFirst)
                _ = sb.Append(',');
            AppendEscaped(sb, key);
            _ = sb.Append(':')
                .Append(value.ToString());
        }

        private static void AppendEscaped(StringBuilder sb, string? value)
        {
            if (value is null)
            {
                _ = sb.Append("null");
                return;
            }

            _ = sb.Append('"');
            foreach (var c in value)
                if (c is '"' or '\\' or '\n' or '\r' or '\t' or '\b' or '\f' or < ' ')
                    _ = sb.Append(c switch
                    {
                        '"' => @"\""",
                        '\\' => @"\\",
                        '\n' => @"\n",
                        '\r' => @"\r",
                        '\t' => @"\t",
                        '\b' => @"\b",
                        '\f' => @"\f",
                        _ => $"\\u{(int)c:x4}",
                    });
                else
                    _ = sb.Append(c);

            _ = sb.Append('"');
        }
    }
}
