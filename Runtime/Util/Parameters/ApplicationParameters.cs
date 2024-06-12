using System;
using UnityEngine;

namespace Elympics
{
    internal static class ApplicationParameters
    {
        public static bool ShouldLoadElympicsOnlineServer => Parameters.IsElympicsOnlineServer.GetValue() is true
            && Parameters.IsElympicsOnlineBot.GetValue() is not true;
        public static bool ShouldLoadElympicsOnlineBot => Parameters.IsElympicsOnlineServer.GetValue() is true
            && Parameters.IsElympicsOnlineBot.GetValue() is true;

        public static ParameterList Parameters
        {
            get
            {
                if (!ParameterList.Initialized && !Application.isEditor)
                    ElympicsLogger.LogWarning("Accessing uninitialized application parameters.\n" + Environment.StackTrace);
                return ParameterList;
            }
        }
        private static readonly ParameterList ParameterList = new();

        internal static bool InitializeParameters()
        {
            var envVariables = Environment.GetEnvironmentVariables();
            var commandLineArgs = Environment.GetCommandLineArgs();
            var urlQuery = ElympicsWebGL.GetUrlQuery();
            return ParameterList.Initialize(envVariables, commandLineArgs, urlQuery);
        }
    }
}
