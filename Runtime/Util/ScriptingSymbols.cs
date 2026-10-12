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
    }
}
