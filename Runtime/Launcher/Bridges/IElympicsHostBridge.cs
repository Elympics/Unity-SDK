#nullable enable

using System;

namespace Elympics
{
    /// <summary>Connects <see cref="ElympicsMatchLauncher"/> to an out-of-process host. Events are sent regardless of who started the action.</summary>
    internal interface IElympicsHostBridge : IDisposable
    {
        string Name { get; }

        /// <summary>Returns false if the transport is not available here (e.g. the native library is not in the build).</summary>
        bool TryStart(ElympicsMatchLauncher launcher);

        /// <summary>Called every frame on the main thread.</summary>
        void Poll();

        void DispatchEvent(string type, string requestId, string payloadJson);
    }
}
