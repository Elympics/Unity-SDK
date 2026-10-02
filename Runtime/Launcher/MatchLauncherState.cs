using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    public enum MatchLauncherState
    {
        Idle,
        Loading,
        Running,
        /// <summary>The gameplay scene stays loaded until the host returns to boot.</summary>
        Ended,
        Unloading,
    }
}
