namespace Elympics
{
    internal static class ScriptingSymbols
    {
        public static bool IsUnityServer =>
#if UNITY_SERVER
            true;
#else
            false;
#endif

        public static bool IsElympicsDebug =>
#if ELYMPICS_DEBUG
            true;
#else
            false;
#endif

#if ELYMPICS_DISABLE_MPPM_AUTO_LAUNCH
        public const bool MppmAutoLaunchDisabled = true;
#else
        public const bool MppmAutoLaunchDisabled = false;
#endif

#if ELYMPICS_DISABLE_LAUNCH_MODE_SCENE_LOAD
        public const bool LaunchModeSceneLoadDisabled = true;
#else
        public const bool LaunchModeSceneLoadDisabled = false;
#endif
    }
}
