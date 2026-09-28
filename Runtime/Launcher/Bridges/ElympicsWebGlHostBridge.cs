#nullable enable

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Linq;
using System.Runtime.InteropServices;
#endif

namespace Elympics
{
    /// <summary>
    /// Host bridge for a web page: installs <c>window.elympicsHost</c> (see ElympicsHostBridge.jslib).
    /// Commands arrive through <c>SendMessage</c> on the launcher's game object, handled by <see cref="ElympicsMatchLauncherBehaviour"/>.
    /// </summary>
    internal sealed class ElympicsWebGlHostBridge : IElympicsHostBridge
    {
        public string Name => "webgl";

#if UNITY_WEBGL && !UNITY_EDITOR
        private ElympicsMatchLauncher? _launcher;

        [DllImport("__Internal")] private static extern void ElympicsHost_Initialize(string gameObjectName);
        [DllImport("__Internal")] private static extern void ElympicsHost_DispatchEvent(string type, string requestId, string payloadJson);
        [DllImport("__Internal")] private static extern string ElympicsHost_TakeLaunchData(string defaultGameId);

        public bool TryStart(ElympicsMatchLauncher launcher)
        {
            _launcher = launcher;
            ElympicsHost_Initialize(ElympicsMatchLauncherBehaviour.GameObjectName);
            return true;
        }

        public void DispatchEvent(string type, string requestId, string payloadJson) => ElympicsHost_DispatchEvent(type, requestId, payloadJson);

        /// <summary>Launches straight into a match if the page set <c>window.elympicsLaunchData</c> before the build loaded.</summary>
        public void OnReady()
        {
            if (_launcher == null)
                return;
            var defaultGameId = ElympicsConfig.Load()?.AvailableGames.FirstOrDefault(game => game != null)?.GameId ?? string.Empty;
            var requestJson = ElympicsHost_TakeLaunchData(defaultGameId);
            if (!string.IsNullOrEmpty(requestJson))
                _launcher.HandleHostLaunch(requestJson);
        }

        public void Dispose() => _launcher = null;
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
