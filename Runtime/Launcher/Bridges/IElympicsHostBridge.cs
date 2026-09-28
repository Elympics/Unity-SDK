#nullable enable

using System;

namespace Elympics
{
    /// <summary>
    /// Connects <see cref="ElympicsMatchLauncher"/> to an out-of-process host. In-process hosts (PlayPad, menu code)
    /// call the launcher directly and need no bridge. Events are sent regardless of who started the action.
    /// </summary>
    internal interface IElympicsHostBridge : IDisposable
    {
        string Name { get; }

        /// <summary>Returns false if the transport is not available here (e.g. the native library is not in the build).</summary>
        bool TryStart(ElympicsMatchLauncher launcher);

        /// <summary>Called once after the launcher emitted <c>ready</c>.</summary>
        void OnReady();

        /// <summary>Called every frame on the main thread; runs queued host commands.</summary>
        void Poll();

        void DispatchEvent(string type, string requestId, string payloadJson);
    }
}
