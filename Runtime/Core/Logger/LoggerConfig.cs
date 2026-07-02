#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Elympics.Core.Logger.State;
using Object = UnityEngine.Object;

namespace Elympics.Core.Logger
{
    internal struct LoggerConfig
    {
        public const string DefaultServiceName = "ElympicsSdk";

        public bool ConsoleDisabled { get; private set; }
        public bool MonitoringEnabled { get; private set; }
        public bool StacktraceForEverything { get; private set; }
        public LogContext Context { get; private set; }

        public static LoggerConfig New() => new()
        {
            Context = new LogContext(),
        };

        [Pure]
        private LoggerConfig Clone() => new()
        {
            MonitoringEnabled = MonitoringEnabled,
            StacktraceForEverything = StacktraceForEverything,
            Context = Context,
        };

        public struct LogContext : IVisitableState
        {
            public Object? UnityContext  // for Unity Debug.Log with context
            {
                get => _unityContext?.Original;
                set => _unityContext = value is not null
                    ? (value, $"{value.GetType().FullName} {value.name} ({value.GetInstanceID()})")
                    : null;
            }
            private (Object Original, string Stringified)? _unityContext;  // for Unity Debug.Log with context
            public string? MethodName;  // from CallerMemberInfo
            public string? ClassName;  // set manually - expected full name
            public string? ServiceName;  // is logging from game or lobby?
            public IReadOnlyDictionary<string, string>? ExtraContext;

            private static class Names
            {
                public const string UnityContext = "unityContext";
                public const string MethodName = "methodName";
                public const string ClassName = "className";
                public const string ClassNameLegacy = "context";
                public const string ServiceName = "serviceName";
                public const string ServiceNameLegacy = "app";
            }

            public bool Visit(IStateVisitor visitor)
            {
                var visitedAnything = false;
                visitedAnything |= visitor.ProcessOptionalProperty(Names.ServiceName, ServiceName, legacyName: Names.ServiceNameLegacy);
                visitedAnything |= visitor.ProcessOptionalProperty(Names.ClassName, ClassName, legacyName: Names.ClassNameLegacy);
                visitedAnything |= visitor.ProcessOptionalProperty(Names.MethodName, MethodName);
                visitedAnything |= visitor.ProcessOptionalProperty(Names.UnityContext, _unityContext?.Stringified);
                if (ExtraContext is not null && ExtraContext.Count > 0)
                {
                    visitedAnything = true;
                    foreach (var kvp in ExtraContext)
                        visitor.ProcessProperty(kvp.Key, kvp.Value);
                }
                return visitedAnything;
            }
        }

        [Pure]
        public LoggerConfig WithConsoleDisabled()
        {
            var clone = Clone();
            clone.ConsoleDisabled = true;
            return clone;
        }

        [Pure]
        public LoggerConfig WithMonitoringEnabled()
        {
            var clone = Clone();
            clone.MonitoringEnabled = true;
            return clone;
        }

        [Pure]
        public LoggerConfig WithStacktraceForEverything()
        {
            var clone = Clone();
            clone.StacktraceForEverything = true;
            return clone;
        }

        [Pure]
        public LoggerConfig WithUnityContext(Object unityContext)
        {
            var clone = Clone();
            var context = clone.Context;
            context.UnityContext = unityContext;
            clone.Context = context;
            return clone;
        }

        [Pure]
        public LoggerConfig WithMethodName([CallerMemberName] string methodName = "")
        {
            var clone = Clone();
            var context = clone.Context;
            context.MethodName = methodName;
            clone.Context = context;
            return clone;
        }

        /// <param name="type">Class type.</param>
        /// <returns>Current instance.</returns>
        [Pure]
        public LoggerConfig WithClass(Type type)
        {
            var clone = Clone();
            var context = clone.Context;
            context.ClassName = type.FullName ?? type.Name;
            clone.Context = context;
            return clone;
        }

        /// <summary>
        /// <seealso cref="WithElympicsSdkService"/>
        /// <seealso cref="WithElympicsGameService"/>
        /// <seealso cref="WithPlayPadSdkService"/>
        /// </summary>
        /// <param name="serviceName">Source service name.</param>
        /// <returns>Current instance.</returns>
        [Pure]
        public LoggerConfig WithServiceName(string serviceName)
        {
            var clone = Clone();
            var context = clone.Context;
            context.ServiceName = serviceName;
            clone.Context = context;
            return clone;
        }

        /// <summary>
        /// <seealso cref="WithServiceName"/>
        /// <seealso cref="WithElympicsGameService"/>
        /// <seealso cref="WithPlayPadSdkService"/>
        /// </summary>
        /// <returns>Current instance.</returns>
        [Pure] public LoggerConfig WithElympicsSdkService() => WithServiceName(DefaultServiceName);

        /// <summary>
        /// <seealso cref="WithServiceName"/>
        /// <seealso cref="WithElympicsSdkService"/>
        /// <seealso cref="WithPlayPadSdkService"/>
        /// </summary>
        /// <returns>Current instance.</returns>
        [Pure] public LoggerConfig WithElympicsGameService() => WithServiceName("ElympicsGame");

        /// <summary>
        /// <seealso cref="WithServiceName"/>
        /// <seealso cref="WithElympicsSdkService"/>
        /// <seealso cref="WithElympicsGameService"/>
        /// </summary>
        /// <returns>Current instance.</returns>
        [Pure] public LoggerConfig WithPlayPadSdkService() => WithServiceName("PlayPadSdk");

        /// <param name="extraContext">Additional logger context entries.</param>
        /// <returns>Current instance.</returns>
        [Pure]
        public LoggerConfig WithExtraContext(IReadOnlyDictionary<string, string> extraContext)
        {
            var clone = Clone();
            var context = clone.Context;
            context.ExtraContext = extraContext;
            clone.Context = context;
            return clone;
        }
    }
}
