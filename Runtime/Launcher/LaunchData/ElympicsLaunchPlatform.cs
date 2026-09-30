#nullable enable

namespace Elympics
{
    /// <summary>What the platform allows a launch to do. Passed into validation, so tests can check every platform's outcome on any build target.</summary>
    internal readonly struct ElympicsLaunchPlatform
    {
        /// <summary>Can open listening sockets, as a half-remote server needs. False on WebGL and iOS players.</summary>
        public bool CanListen { get; }
        /// <summary>Has TCP/UDP sockets. False with the WebGL build target, where <see cref="ElympicsGameConfig.UseWeb"/> is forced.</summary>
        public bool CanUseTcpUdp { get; }
        /// <summary>Can read local files, e.g. a replay by path. False in WebGL players.</summary>
        public bool HasLocalFiles { get; }

        public ElympicsLaunchPlatform(bool canListen, bool canUseTcpUdp, bool hasLocalFiles)
        {
            CanListen = canListen;
            CanUseTcpUdp = canUseTcpUdp;
            HasLocalFiles = hasLocalFiles;
        }

        /// <summary>This build.</summary>
        public static ElympicsLaunchPlatform Current => new(
#if (UNITY_WEBGL || UNITY_IOS) && !UNITY_EDITOR
            canListen: false,
#else
            canListen: true,
#endif
#if UNITY_WEBGL
            canUseTcpUdp: false,
#else
            canUseTcpUdp: true,
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
            hasLocalFiles: false);
#else
            hasLocalFiles: true);
#endif

        /// <summary>Everything allowed.</summary>
        public static ElympicsLaunchPlatform Unrestricted => new(true, true, true);

        public const string CannotListenMessage = "platform cannot open a listening socket";
        public const string NoTcpUdpMessage = "TCP/UDP is not available on WebGL. Use WebRTC.";
        public const string NoLocalFilesMessage = "must be an http(s) URL on WebGL, which has no local file system";
    }
}
