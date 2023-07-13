using System;
using System.Globalization;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEditor;
using UnityEngine;

namespace Elympics.Editor.Communication.UsageStatistics
{
    [InitializeOnLoad]
    internal static class UsageStatisticsSender
    {
        private const string SessionStartKey = "Elympics/SessionStart";
        private static readonly TimeSpan ComparisonEpsilon = TimeSpan.FromMinutes(1);

        static UsageStatisticsSender()
        {
            if (ElympicsClonesManager.IsClone())
                return;
            OnAssemblyReload();
            EditorApplication.quitting += OnQuitting;
        }

        private static void OnAssemblyReload()
        {
            var currentSessionStart = DateTime.UtcNow - TimeSpan.FromSeconds(EditorApplication.timeSinceStartup);
            if (!HasBeenRestarted(currentSessionStart))
                return;
            ElympicsWebIntegration.PostStartEvent();
            PlayerPrefs.SetString(SessionStartKey, currentSessionStart.ToString("o", CultureInfo.InvariantCulture));
        }

        private static bool HasBeenRestarted(DateTime currentSessionStart)
        {
            var serializedSessionStart = PlayerPrefs.GetString(SessionStartKey) ?? "";
            if (!DateTime.TryParse(serializedSessionStart, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                out var savedSessionStart))
                return true;
            if (savedSessionStart - currentSessionStart > ComparisonEpsilon)
                return true;  // something's wrong - should reset
            return currentSessionStart - savedSessionStart > ComparisonEpsilon;
        }

        private static void OnQuitting() => ElympicsWebIntegration.PostStopEvent();
    }
}
