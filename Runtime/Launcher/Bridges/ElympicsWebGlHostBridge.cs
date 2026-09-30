#nullable enable

#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using AOT;
using Elympics.Core.Logger;
#endif

namespace Elympics
{
    /// <summary>
    /// Host bridge for a web page: installs <c>window.elympicsHost</c> (see ElympicsHostBridge.jslib).
    /// The page's commands arrive through one C# callback registered at start, called synchronously from the page's call.
    /// </summary>
    internal sealed class ElympicsWebGlHostBridge : IElympicsHostBridge
    {
        public string Name => "webgl";

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>Called by the page with a command kind and up to three UTF-8 strings (unused ones are empty).</summary>
        private delegate void CommandCallback(int kind, IntPtr first, IntPtr second, IntPtr third);

        private static ElympicsMatchLauncher? launcher;

        [DllImport("__Internal")] private static extern void ElympicsHost_Initialize(CommandCallback commandCallback);
        [DllImport("__Internal")] private static extern void ElympicsHost_DispatchEvent(string type, string requestId, string payloadJson);

        public bool TryStart(ElympicsMatchLauncher newLauncher)
        {
            launcher = newLauncher;
            ElympicsHost_Initialize(OnCommand);
            return true;
        }

        // The page frees the strings after this returns, so they are copied here.
        [MonoPInvokeCallback(typeof(CommandCallback))]
        private static void OnCommand(int kind, IntPtr first, IntPtr second, IntPtr third)
        {
            try
            {
                launcher?.HandleHostCommand((ElympicsHostCommandKind)kind, ToText(first), ToText(second), ToText(third));
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
            }
        }

        private static string ToText(IntPtr pointer) => pointer == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAuto(pointer) ?? string.Empty;

        public void DispatchEvent(string type, string requestId, string payloadJson) => ElympicsHost_DispatchEvent(type, requestId, payloadJson);

        public void Dispose() => launcher = null;
#else
        public bool TryStart(ElympicsMatchLauncher launcher) => false;

        public void DispatchEvent(string type, string requestId, string payloadJson)
        { }

        public void Dispose()
        { }
#endif

        public void Poll()
        { }
    }
}
