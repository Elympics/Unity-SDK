#if UNITY_EDITOR && ELYMPICS_LAZY_WEBRTC_CONTEXT
#nullable enable

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Elympics.Core.Logger;
using Unity.WebRTC;
using UnityEditor;

namespace Elympics.GameEngine.Libraries.WebRtc
{
    /// <summary>
    /// With ELYMPICS_LAZY_WEBRTC_CONTEXT, keeps com.unity.webrtc's native context stopped in the Editor until Elympics creates a peer connection.
    /// com.unity.webrtc 3.0 starts it after every assembly reload, and its native threads can crash Unity during a later reload or on quit.
    /// Uses com.unity.webrtc internals; when they are missing, the package's own behaviour is kept.
    /// </summary>
    [InitializeOnLoad]
    internal static class EditorWebRtcContext
    {
        private const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? ContextField = typeof(WebRTC).GetField("s_context", StaticMembers);
        private static readonly MethodInfo? InitializeMethod = typeof(WebRTC).GetMethod("InitializeInternal", StaticMembers);
        private static readonly MethodInfo? DisposeMethod = typeof(WebRTC).GetMethod("DisposeInternal", StaticMembers);
        private static readonly Type? ContextManagerType = typeof(WebRTC).Assembly.GetType("Unity.WebRTC.ContextManager");

        internal static bool IsSupported { get; }

        static EditorWebRtcContext()
        {
            var onBeforeAssemblyReload = ContextManagerType?.GetMethod("OnBeforeAssemblyReload", StaticMembers);
            var onAfterAssemblyReload = ContextManagerType?.GetMethod("OnAfterAssemblyReload", StaticMembers);
            var onQuit = ContextManagerType?.GetMethod("Quit", StaticMembers);
            if (ContextField == null || InitializeMethod == null || DisposeMethod == null || ContextManagerType == null
                || onBeforeAssemblyReload == null || onAfterAssemblyReload == null || onQuit == null
                || !InitializeMethod.GetParameters().All(parameter => parameter.HasDefaultValue))
            {
                ElympicsLogger.LogWarning("ELYMPICS_LAZY_WEBRTC_CONTEXT has no effect: this com.unity.webrtc version doesn't have the expected internals.");
                return;
            }
            IsSupported = true;

            // Runs its static constructor now, so the handlers it subscribes there can be removed.
            RuntimeHelpers.RunClassConstructor(ContextManagerType.TypeHandle);
            AssemblyReloadEvents.beforeAssemblyReload -= CreateDelegate<AssemblyReloadEvents.AssemblyReloadCallback>(onBeforeAssemblyReload);
            AssemblyReloadEvents.afterAssemblyReload -= CreateDelegate<AssemblyReloadEvents.AssemblyReloadCallback>(onAfterAssemblyReload);
            EditorApplication.quitting -= CreateDelegate<Action>(onQuit);

            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
        }

        internal static bool IsInitialized => ContextField?.GetValue(null) != null;

        internal static void EnsureInitialized()
        {
            if (!IsSupported || IsInitialized)
                return;
            _ = InitializeMethod!.Invoke(null, InitializeMethod.GetParameters().Select(parameter => parameter.DefaultValue).ToArray());
        }

        private static void Dispose()
        {
            if (IsInitialized)
                _ = DisposeMethod!.Invoke(null, null);
        }

        private static T CreateDelegate<T>(MethodInfo method) where T : Delegate => (T)Delegate.CreateDelegate(typeof(T), method);
    }
}
#else
namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal static class EditorWebRtcContext
    {
        internal static void EnsureInitialized()
        { }
    }
}
#endif
