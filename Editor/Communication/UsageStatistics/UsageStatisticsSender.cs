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
            OnAssemblyReload();
            EditorApplication.quitting += OnQuitting;
        }

        private static void OnAssemblyReload()
        {
            if (SessionState.GetBool(SessionStartKey, false))
                return;
            ElympicsWebIntegration.PostStartEvent();
            SessionState.SetBool(SessionStartKey, true);
        }

        private static void OnQuitting() => ElympicsWebIntegration.PostStopEvent();
    }
}
