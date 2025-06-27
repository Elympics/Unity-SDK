namespace Elympics.Util
{
    internal static class UnityUtil
    {
        public static bool IsEditor =>
#if UNITY_EDITOR
                true;
#else
                false;
#endif


        public static bool IsWebGL =>
#if UNITY_WEBGL_API && !UNITY_EDITOR
                true;
#else
                false;
#endif

    }
}
