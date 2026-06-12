#nullable enable
using System;
using System.Text;
using System.Text.RegularExpressions;
using Elympics.AssemblyCommunicator;
using Elympics.Core.Logger.State;
using Elympics.Events;
using UnityEngine;

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
                _ = _stringBuilder.Clear();
                AppendProperty(_stringBuilder, nameof(time), stringifiedTime, isFirst: true);
                AppendProperty(_stringBuilder, nameof(message), message);
                if (stacktrace is not null)
                    AppendProperty(_stringBuilder, nameof(stacktrace), stacktrace);
                _ = config.Context.Visit(_stateVisitor);
                _ = state.Visit(_stateVisitor);
                finalMessage = _stringBuilder.ToString();
            }

            CrossAssemblyEventBroadcaster.RaiseEvent(new ElympicsLogEvent
            {
                LogLevel = category switch
                {
                    LogCategory.Exception => LogLevel.Exception,
                    LogCategory.Error => LogLevel.Error,
                    LogCategory.Warning => LogLevel.Warning,
                    LogCategory.Info or LogCategory.Debug or LogCategory.Trace => LogLevel.Log,
                    _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
                },
                Time = stringifiedTime,
                Message = finalMessage,
            });
        }


        private class JsonStateVisitor : IStateVisitor
        {
            private readonly StringBuilder _stringBuilder;

            public JsonStateVisitor(StringBuilder stringBuilder) => _stringBuilder = stringBuilder;

            public void ProcessSubstate(string name) { }

            public void ProcessProperty(string name, string value) => AppendProperty(_stringBuilder, name, value);
        }

        private static void AppendProperty(StringBuilder sb, string key, string value, bool isFirst = false)
        {
            if (!isFirst)
                _ = sb.Append(',');
            _ = sb.Append(StringValue.ToJson(key))
                .Append(':')
                .Append(StringValue.ToJson(value));
        }

        [Serializable]
        private struct StringValue
        {
            private static readonly Regex ValueRegex = new(@"^\s*\{\s*""" + nameof(v) + @"""\s*:\s*("".*"")\s*\}\s*$", RegexOptions.Compiled);
            [SerializeField] private string v;
            private StringValue(string value) => v = value;
            public static string ToJson(string value) => ValueRegex.Match(JsonUtility.ToJson(new StringValue(value))).Groups[1].Value;
        }
    }
}
