#nullable enable
using System;
using System.Text;
using Elympics.AssemblyCommunicator;
using Elympics.Core.Logger.State;
using Elympics.Core.Utils;
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
                    .Append('{')
                    .AppendJsonProperty("level", (int)logLevel, isFirst: true)
                    .AppendJsonProperty("message", message);
                if (stacktrace is not null)
                    _ = _stringBuilder.AppendJsonProperty(nameof(stacktrace), stacktrace);
                _ = _stringBuilder.Append(",\"data\":{")
                    .AppendJsonProperty("time", isoTimestamp, isFirst: true);
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

            public void ProcessSubstate(string name) { }

            public void ProcessProperty(string name, string value, string? legacyName = null)
            {
                _ = _stringBuilder.AppendJsonProperty(name, value);
                if (legacyName is not null)
                    _ = _stringBuilder.AppendJsonProperty(legacyName, value);
            }
        }
    }
}
