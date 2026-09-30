#nullable enable

#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Linq;
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
        [DllImport("__Internal")] private static extern string ElympicsHost_TakeLaunchData(string defaultGameId);

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
                launcher?.HandleHostCommand((ElympicsHostCommandKind)kind, Marshal.PtrToStringAuto(first) ?? string.Empty);
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
            }
        }

        public void DispatchEvent(string type, string requestId, string payloadJson) => ElympicsHost_DispatchEvent(type, requestId, payloadJson);

        /// <summary>Launches straight into a match if the page set <c>window.elympicsLaunchData</c> before the build loaded.</summary>
        public void OnReady()
        {
            if (launcher == null)
                return;
            var defaultGameId = ElympicsConfig.Load()?.AvailableGames.FirstOrDefault(game => game != null)?.GameId ?? string.Empty;
            var requestJson = ElympicsHost_TakeLaunchData(defaultGameId);
            if (!string.IsNullOrEmpty(requestJson))
                launcher.HandleHostCommand(ElympicsHostCommandKind.LaunchMatch, requestJson);
        }

        public void Dispose() => launcher = null;
#else
        public bool TryStart(ElympicsMatchLauncher launcher) => false;

        public void DispatchEvent(string type, string requestId, string payloadJson)
        { }

        public void OnReady()
        { }

        public void Dispose()
        { }
#endif

        public void Poll()
        { }
    }
}
