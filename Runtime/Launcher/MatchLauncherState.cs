using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    public enum MatchLauncherState
    {
        /// <summary>In the boot scene, ready to launch.</summary>
        Idle,
        /// <summary>Loading the gameplay scene and connecting.</summary>
        Loading,
        /// <summary>The match is being played.</summary>
        Running,
        /// <summary>The match ended or the connection was lost. The gameplay scene stays loaded until the host returns to boot.</summary>
        Ended,
        /// <summary>Tearing the match down and returning to the boot scene.</summary>
        Unloading,
    }
}
