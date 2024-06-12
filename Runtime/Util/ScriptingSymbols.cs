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
    }
}
