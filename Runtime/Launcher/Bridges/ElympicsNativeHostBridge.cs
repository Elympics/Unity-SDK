#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Text;
using Elympics.Core.Logger;

namespace Elympics
{
    /// <summary>
    /// Host bridge for a native app embedding Unity as a library (iOS, Windows, Linux, macOS),
    /// through the C API in <c>Runtime/Launcher/Native/ElympicsHost.h</c>.
    /// The host's commands are queued by the plugin and drained here on the main thread.
    /// </summary>
    internal sealed class ElympicsNativeHostBridge : IElympicsHostBridge
    {
        /// <summary>Must match <c>ApiVersion</c> in ElympicsHost.cpp.</summary>
        internal const int ApiVersion = 1;

        /// <summary>Bounds the work per frame if a host floods commands.</summary>
        private const int MaxCommandsPerFrame = 64;

        private enum CommandKind
        {
            None = 0,
            LaunchMatch = 1,
            AbortMatch = 2,
            ReturnToBoot = 3,
            QuitApplication = 4,
            Ping = 5,
        }

#if UNITY_IOS && !UNITY_EDITOR
        private const string Library = "__Internal";
#else
        private const string Library = "ElympicsHost";
#endif

        [DllImport(Library)] private static extern int ElympicsHost_GetApiVersion();
        [DllImport(Library)] private static extern int ElympicsHost_DispatchEvent(byte[] type, byte[] requestId, byte[] payloadJson);
        [DllImport(Library)] private static extern IntPtr ElympicsHost_PollCommand(out int kind);

        public string Name => "native";

        private readonly LoggerConfig _logger = ElympicsLogger.WithClass(typeof(ElympicsNativeHostBridge));
        private ElympicsMatchLauncher? _launcher;
        private bool _warnedNoHandler;

        public bool TryStart(ElympicsMatchLauncher launcher)
        {
            int version;
            try
            {
                version = ElympicsHost_GetApiVersion();
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
                _logger.LogInfo($"Native host library {Library} is not available ({e.GetType().Name}). No native host bridge.");
                return false;
            }
            if (version != ApiVersion)
            {
                _logger.LogError($"Native host library {Library} has API version {version}, expected {ApiVersion}. No native host bridge.");
                return false;
            }
            _launcher = launcher;
            return true;
        }

        public void OnReady()
        { }

        public void Poll()
        {
            if (_launcher == null)
                return;
            for (var i = 0; i < MaxCommandsPerFrame; i++)
            {
                var argumentPointer = ElympicsHost_PollCommand(out var kind);
                if ((CommandKind)kind == CommandKind.None)
                    return;
                // Copied right away: the plugin reuses the buffer on the next poll.
                var argument = FromUtf8(argumentPointer);
                try
                {
                    Execute((CommandKind)kind, argument);
                }
                catch (Exception e)
                {
                    _logger.LogException(e);
                }
            }
        }

        private void Execute(CommandKind kind, string argument)
        {
            switch (kind)
            {
                case CommandKind.LaunchMatch:
                    _launcher!.HandleHostLaunch(argument);
                    break;
                case CommandKind.AbortMatch:
                case CommandKind.ReturnToBoot:
                    _launcher!.HandleHostAbort(argument);
                    break;
                case CommandKind.QuitApplication:
                    _launcher!.HandleHostQuit(argument);
                    break;
                case CommandKind.Ping:
                    _launcher!.HandleHostPing(argument);
                    break;
                case CommandKind.None:
                default:
                    _logger.LogWarning($"Unknown native host command {(int)kind} ignored.");
                    break;
            }
        }

        public void DispatchEvent(string type, string requestId, string payloadJson)
        {
            if (ElympicsHost_DispatchEvent(ToUtf8(type), ToUtf8(requestId), ToUtf8(payloadJson)) != 0 || _warnedNoHandler)
                return;
            _warnedNoHandler = true;
            _logger.LogWarning($"Native host has no event handler registered (ElympicsHost_RegisterEventHandler). Dropping event {type} and later ones.");
        }

        public void Dispose() => _launcher = null;

        // Marshalled by hand: default string marshalling is ANSI on Windows, and the C API is UTF-8 everywhere.
        private static byte[] ToUtf8(string? value)
        {
            value ??= string.Empty;
            var bytes = new byte[Encoding.UTF8.GetByteCount(value) + 1];
            _ = Encoding.UTF8.GetBytes(value, 0, value.Length, bytes, 0);
            return bytes;
        }

        private static string FromUtf8(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
                return string.Empty;
            var length = 0;
            while (Marshal.ReadByte(pointer, length) != 0)
                length++;
            var bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
