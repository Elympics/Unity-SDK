#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEditor;
#else
using UnityEngine;
#endif

namespace Elympics
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    internal static class ExitUtility
    {
        private static bool isInPlayMode;

        static ExitUtility() => EditorApplication.playModeStateChanged += change =>
        {
            if (change == PlayModeStateChange.EnteredEditMode)
                isInPlayMode = false;
            else if (change == PlayModeStateChange.EnteredPlayMode)
                isInPlayMode = true;
        };

        public static void ExitGame() => ExitPlaymode().Forget();

        private static async UniTask ExitPlaymode()
        {
            while (!isInPlayMode)
                await UniTask.Delay(TimeSpan.FromMilliseconds(500), DelayType.Realtime);
            EditorApplication.ExitPlaymode();
        }
    }
#else
    internal static class ExitUtility
    {
        public static void ExitGame() => Application.Quit();
    }
#endif
}
