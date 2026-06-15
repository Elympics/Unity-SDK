#nullable enable
using System;
using System.Text;
using Elympics.Core.Logger.State;
using UnityEngine;

namespace Elympics.Core.Logger.Builder
{
    internal class PlainLogOutlet : ILogOutlet
    {
        private const string StringPrefixFormat = "[{0}] ";
        private const string StateHeader = "=== Current application state ===\n";

        private static readonly object StringBuilderLock = new();
        private readonly StringBuilder _stringBuilder;
        private readonly PlainStateVisitor _stateVisitor;

        public PlainLogOutlet()
        {
            _stringBuilder = new StringBuilder();
            _stateVisitor = new PlainStateVisitor(_stringBuilder);
        }

        public void Log(
            LogCategory category,
            DateTime time,
            string message,
            string? stacktrace,
            ApplicationState state,
            LoggerConfig config)
        {
            if (config.ConsoleDisabled)
                return;
            var context = config.Context;
            var shouldLogStacktrace = category is LogCategory.Exception || config.StacktraceForEverything;
            var logCategoryPrefix = category switch
            {
                LogCategory.Exception => "EXC",
                LogCategory.Error => "ERR",
                LogCategory.Warning => "WARN",
                LogCategory.Info => "INFO",
                LogCategory.Debug => "DEBUG",
                LogCategory.Trace => "TRACE",
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };
            string finalMessage;
            lock (StringBuilderLock)
            {
                _ = _stringBuilder.Clear()
#if !UNITY_EDITOR
                    .AppendFormat(StringPrefixFormat, time)
#endif
                    .AppendFormat(StringPrefixFormat, logCategoryPrefix)
                    .AppendFormat(StringPrefixFormat, !string.IsNullOrEmpty(context.ServiceName) ? context.ServiceName : LoggerConfig.DefaultServiceName)
                    .Append(message);
                AppendContextAndState(config.Context, state);
#if !UNITY_EDITOR
                if (!string.IsNullOrEmpty(stacktrace))
                {
                    _ = _stringBuilder.AppendLine().Append(stacktrace);
                    shouldLogStacktrace = false;
                }
#endif
                finalMessage = _stringBuilder.AppendLine().ToString();
            }

            var logType = category.ToLogType();
            Debug.LogFormat(logType, shouldLogStacktrace ? LogOption.None : LogOption.NoStacktrace, context.LinkedObject, "{0}", finalMessage);
        }

        private void AppendContextAndState(LoggerConfig.LogContext context, ApplicationState state)
        {
            _ = _stringBuilder.Append("\n\n");
            _ = _stringBuilder.Append(StateHeader);
            _stateVisitor.Reset();
            _stateVisitor.RequiresSubstateSeparator = context.Visit(_stateVisitor);
            _ = state.Visit(_stateVisitor);
        }

        private class PlainStateVisitor : IStateVisitor
        {
            private readonly StringBuilder _stringBuilder;
            private string? _currentSubstate;
            private bool _requiresPropertySeparator;
            public bool RequiresSubstateSeparator { private get; set; }

            private const string SubstateFormat = "[{0}] ";
            private const string PropertyFormat = "{0}: {1}";
            private const string SubstateSeparator = "\n";
            private const string PropertySeparator = " | ";

            public PlainStateVisitor(StringBuilder stringBuilder) => _stringBuilder = stringBuilder;

            public void ProcessSubstate(string name)
            {
                if (_currentSubstate == name)
                    return;
                _currentSubstate = name;
                _requiresPropertySeparator = false;
                if (RequiresSubstateSeparator)
                    _ = _stringBuilder.Append(SubstateSeparator);
                RequiresSubstateSeparator = true;
                _ = _stringBuilder.AppendFormat(SubstateFormat, name);
            }

            public void ProcessProperty(string name, string value)
            {
                if (_requiresPropertySeparator)
                    _ = _stringBuilder.Append(PropertySeparator);
                _requiresPropertySeparator = true;
                _ = _stringBuilder.AppendFormat(PropertyFormat, name, value);
            }

            public void Reset()
            {
                _currentSubstate = null;
                _requiresPropertySeparator = false;
            }
        }
    }
}
