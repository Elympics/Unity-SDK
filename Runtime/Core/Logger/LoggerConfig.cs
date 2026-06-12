#nullable enable
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Elympics.Core.Logger.State;
using Object = UnityEngine.Object;

namespace Elympics.Core.Logger
{
    internal struct LoggerConfig
    {
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
            public Object? LinkedObject;  // for Unity Debug.Log with context
            public string? MethodName;  // from CallerMemberInfo
            public string? ClassName;  // set manually - expected full name
            public string? ServiceName;  // is logging from game or lobby?

            public bool Visit(IStateVisitor visitor)
            {
                var visitedAnything = false;
                if (ServiceName is not null)
                {
                    visitor.ProcessProperty(nameof(ServiceName), ServiceName);
                    visitedAnything = true;
                }

                if (ClassName is not null)
                {
                    visitor.ProcessProperty(nameof(ClassName), ClassName);
                    visitedAnything = true;
                }

                if (MethodName is not null)
                {
                    visitor.ProcessProperty(nameof(MethodName), MethodName);
                    visitedAnything = true;
                }

                if (LinkedObject is not null)
                {
                    visitor.ProcessProperty(nameof(LinkedObject), $"{LinkedObject.GetType().FullName} {LinkedObject.name} ({LinkedObject.GetInstanceID()})");
                    visitedAnything = true;
                }

                return visitedAnything;
            }
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
            context.LinkedObject = unityContext;
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
        [Pure] public LoggerConfig WithElympicsSdkService() => WithServiceName("ElympicsSdk");

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
    }
}
