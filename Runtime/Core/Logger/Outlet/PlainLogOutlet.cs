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
        private const string DefaultServiceName = "ElympicsSdk";
        private const string StateHeader = "=== Current application state ===\n";

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
            var context = config.Context;
            _ = _stringBuilder.Clear()
#if !UNITY_EDITOR
                .AppendFormat(StringPrefixFormat, time);
#endif
                .AppendFormat(StringPrefixFormat, !string.IsNullOrEmpty(context.ServiceName) ? context.ServiceName : DefaultServiceName)
                .Append(message);
            if (!string.IsNullOrEmpty(stacktrace))
                _ = _stringBuilder.AppendLine().Append(stacktrace);
            // TODO: append config.context ~dsygocki 2026-05-26
            AppendContextAndState(config.Context, state);
            var finalMessage = _stringBuilder.ToString();

            switch (category)
            {
                case LogCategory.Exception:
                case LogCategory.Error:
                {
                    Debug.LogError(finalMessage, context.LinkedObject);
                    break;
                }
                case LogCategory.Warning:
                {
                    Debug.LogWarning(finalMessage, context.LinkedObject);
                    break;
                }
                case LogCategory.Debug:
                case LogCategory.Info:
                case LogCategory.Trace:
                {
                    Debug.Log(finalMessage, context.LinkedObject);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        private void AppendContextAndState(LoggerConfig.LogContext context, ApplicationState state)
        {
            _ = _stringBuilder.Append("\n\n");
            context.Visit(_stateVisitor);
            _ = _stringBuilder.Append(StateHeader);
            state.Visit(_stateVisitor);
        }

        private class PlainStateVisitor : IStateVisitor
        {
            private readonly StringBuilder _stringBuilder;
            private string? _currentSubstate;
            private bool _requiresSeparator;

            private const string SubstateFormat = "[{0}]";
            private const string PropertyFormat = "{0}: {1}";
            private const string Separator = " | ";

            public PlainStateVisitor(StringBuilder stringBuilder) => _stringBuilder = stringBuilder;

            public void ProcessSubstate(string name)
            {
                if (_currentSubstate == name)
                    return;
                _currentSubstate = name;
                _requiresSeparator = false;
                _ = _stringBuilder.AppendFormat(SubstateFormat, name);
            }

            public void ProcessProperty(string name, string value)
            {
                if (_requiresSeparator)
                    _ = _stringBuilder.Append(Separator);
                _ = _stringBuilder.AppendFormat(PropertyFormat, name, value);
                _requiresSeparator = true;
            }
        }
    }
}
