using System;
using Elympics.Core.Logger;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEditor;

namespace Elympics.Editor.Communication.UsageStatistics
{
    [InitializeOnLoad]
    internal static class UsageStatisticsSender
    {
        private const string SessionStartKey = "Elympics/SessionStart";

        static UsageStatisticsSender()
        {
            if (ElympicsClonesManager.IsClone())
                return;
            if (Environment.GetEnvironmentVariable("CI") is not null)
                return;
            OnAssemblyReload();
            EditorApplication.quitting += OnQuitting;
        }

        private static void OnAssemblyReload()
        {
            if (SessionState.GetBool(SessionStartKey, false) || ElympicsConfig.Load() == null)
                return;
            try
            {
                ElympicsWebIntegration.PostStartEvent();
                SessionState.SetBool(SessionStartKey, true);
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
            }
        }

        private static void OnQuitting()
        {
            try
            {
                ElympicsWebIntegration.PostStopEvent();
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
            }
        }
    }
}
