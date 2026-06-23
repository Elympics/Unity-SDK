#nullable enable
using System;
using System.Text;
using Elympics.AssemblyCommunicator;
using Elympics.Core.Logger.State;
using Elympics.Events;

namespace Elympics.Core.Logger.Builder
{
    internal class JsonLogOutlet : ILogOutlet
    {
        private static readonly object StringBuilderLock = new();
        private readonly StringBuilder _stringBuilder;
        private readonly JsonStateVisitor _stateVisitor;

        public JsonLogOutlet()
        {
            _stringBuilder = new StringBuilder();
            _stateVisitor = new JsonStateVisitor(_stringBuilder);
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

            var stringifiedTime = TimeUtil.DateTimeToString(time);
            string finalMessage;
            lock (StringBuilderLock)
            {
                _ = _stringBuilder.Clear()
                    .Append('{');
                AppendProperty(_stringBuilder, "time", stringifiedTime, isFirst: true);
                AppendProperty(_stringBuilder, "level", category.ToString());
                AppendProperty(_stringBuilder, "message", message);
                if (stacktrace is not null)
                    AppendProperty(_stringBuilder, nameof(stacktrace), stacktrace);
                _ = config.Context.Visit(_stateVisitor);
                _ = state.Visit(_stateVisitor);
                finalMessage = _stringBuilder.Append('}')
                    .ToString();
            }

            CrossAssemblyEventBroadcaster.RaiseEvent(new ElympicsLogEvent
            {
                LogLevel = category.ToLogLevel(),
                Time = stringifiedTime,
                Json = finalMessage,
            });
        }


        private class JsonStateVisitor : IStateVisitor
        {
            private readonly StringBuilder _stringBuilder;

            public JsonStateVisitor(StringBuilder stringBuilder) => _stringBuilder = stringBuilder;

            public void ProcessSubstate(string name) { }

            public void ProcessProperty(string name, string value) => AppendProperty(_stringBuilder, StartingWithLowercase(name), value);

            private static string StartingWithLowercase(string source) => source.Length > 0 && char.IsUpper(source[0])
                ? source[..1].ToLower() + source[1..]
                : source;
        }

        private static void AppendProperty(StringBuilder sb, string key, string? value, bool isFirst = false)
        {
            if (!isFirst)
                _ = sb.Append(',');
            AppendEscaped(sb, key);
            _ = sb.Append(':');
            AppendEscaped(sb, value);
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
