#nullable enable

#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using AOT;
using Elympics.Core.Logger;
#endif

namespace Elympics
{
    internal sealed class ElympicsWebGlHostBridge : IElympicsHostBridge
    {
        public string Name => ElympicsHostBridgeFactory.WebGl;

#if UNITY_WEBGL && !UNITY_EDITOR
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
                launcher?.HandleHostCommand((ElympicsHostCommandKind)kind, HostBridgeText.FromUtf8(first), HostBridgeText.FromUtf8(second), HostBridgeText.FromUtf8(third));
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
            }
        }

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
