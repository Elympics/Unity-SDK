#nullable enable
using System.Runtime.CompilerServices;
using Elympics.Core.Logger.State;
using UnityEngine;

namespace Elympics.Core.Logger
{
    internal class ElympicsLoggerConfig
    {
        public bool MonitoringEnabled { get; set; }
        public bool StacktraceForEverything { get; set; }
        public LogContext Context { get; set; } = new();

        public class LogContext : IVisitableState
        {
            public Object? LinkedObject;  // for Unity Debug.Log with context
            public string? MethodName;  // from CallerMemberInfo
            public string? ClassName;  // set manually - expected full name
            public string? ServiceName;  // is logging from game or lobby?

            public void Visit(IStateVisitor visitor)
            {
                if (ServiceName is not null)
                    visitor.ProcessProperty(nameof(ServiceName), ServiceName);
                if (ClassName is not null)
                    visitor.ProcessProperty(nameof(ClassName), ClassName);
                if (MethodName is not null)
                    visitor.ProcessProperty(nameof(MethodName), MethodName);
                if (LinkedObject is not null)
                    visitor.ProcessProperty(nameof(LinkedObject), $"{LinkedObject.GetType().FullName} {LinkedObject.name} ({LinkedObject.GetInstanceID()})");
            }
        }

        public ElympicsLoggerConfig WithMonitoringEnabled()
        {
            MonitoringEnabled = true;
            return this;
        }

        public ElympicsLoggerConfig WithStacktraceForEverything()
        {
            StacktraceForEverything = true;
            return this;
        }

        public ElympicsLoggerConfig WithUnityContext(Object context)
        {
            Context.LinkedObject = context;
            return this;
        }

        public ElympicsLoggerConfig WithMehodName([CallerMemberName] string methodName = "")
        {
            Context.MethodName = methodName;
            return this;
        }

        /// <param name="className">Should be full class name with namespace included.</param>
        /// <returns>Current instance.</returns>
        public ElympicsLoggerConfig WithClassName(string className)
        {
            Context.ClassName = className;
            return this;
        }

        /// <summary>
        /// <seealso cref="WithElympicsSdkService"/>
        /// <seealso cref="WithElympicsGameService"/>
        /// <seealso cref="WithPlayPadSdkService"/>
        /// </summary>
        /// <param name="serviceName">Source service name.</param>
        /// <returns>Current instance.</returns>
        public ElympicsLoggerConfig WithServiceName(string serviceName)
        {
            Context.ServiceName = serviceName;
            return this;
        }

        /// <summary>
        /// <seealso cref="WithServiceName"/>
        /// <seealso cref="WithElympicsGameService"/>
        /// <seealso cref="WithPlayPadSdkService"/>
        /// </summary>
        /// <returns>Current instance.</returns>
        public ElympicsLoggerConfig WithElympicsSdkService()
        {
            Context.ServiceName = "ElympicsSdk";
            return this;
        }

        /// <summary>
        /// <seealso cref="WithServiceName"/>
        /// <seealso cref="WithElympicsSdkService"/>
        /// <seealso cref="WithPlayPadSdkService"/>
        /// </summary>
        /// <returns>Current instance.</returns>
        public ElympicsLoggerConfig WithElympicsGameService()
        {
            Context.ServiceName = "ElympicsGame";
            return this;
        }

        /// <summary>
        /// <seealso cref="WithServiceName"/>
        /// <seealso cref="WithElympicsSdkService"/>
        /// <seealso cref="WithElympicsGameService"/>
        /// </summary>
        /// <returns>Current instance.</returns>
        public ElympicsLoggerConfig WithPlayPadSdkService()
        {
            Context.ServiceName = "PlayPadSdk";
            return this;
        }
    }
}
