using System;
using Elympics.Core.Logger;
using UnityEngine;

namespace Elympics
{
    internal static class ApplicationParameters
    {
        public static bool ShouldLoadElympicsOnlineServer => Parameters.IsElympicsOnlineServer.GetValue() is true;

        public static bool IsInitialized => ParameterList.Initialized;

        public static ParameterList Parameters
        {
            get
            {
                if (!ParameterList.Initialized && !Application.isEditor)
                    ElympicsLogger.WithStacktraceForEverything().LogWarning("Accessing uninitialized application parameters.");
                return ParameterList;
            }
        }
        private static readonly ParameterList ParameterList = new();

        internal static bool InitializeParameters()
        {
            var envVariables = Environment.GetEnvironmentVariables();
            var commandLineArgs = Environment.GetCommandLineArgs();
            var urlQuery = ElympicsJsApi.GetUrlQuery();
            var elympicsUrlQuery = ElympicsJsApi.GetElympicsUrlQuery();
            return ParameterList.Initialize(envVariables, commandLineArgs, urlQuery, elympicsUrlQuery, PlayModeArguments.Read());
        }
    }
}
