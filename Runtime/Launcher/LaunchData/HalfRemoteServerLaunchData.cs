#nullable enable

using System;
using System.Net;
using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>Transports a half-remote server listens on.</summary>
    [Flags, PublicAPI]
    public enum ElympicsHalfRemoteProtocols
    {
        TcpUdp = 1,
        WebRtc = 2,
        Both = TcpUdp | WebRtc,
    }

    /// <summary>
    /// Runs a half-remote server in this process, which half-remote clients (see <see cref="HalfRemoteClientLaunchData"/>) join.
    /// Every property is optional: null means the value from <see cref="ElympicsGameConfig"/>. Not available on WebGL and iOS.
    /// </summary>
    [PublicAPI]
    public sealed class HalfRemoteServerLaunchData : LaunchData
    {
        /// <summary>Null means <see cref="ElympicsHalfRemoteProtocols.Both"/>.</summary>
        public ElympicsHalfRemoteProtocols? Protocols { get; }
        /// <summary>IP address to listen on. Null means <see cref="ElympicsGameConfig.IpForHalfRemoteMode"/>.</summary>
        public string? ServerHostname { get; }
        /// <summary>Used only when listening on <see cref="ElympicsHalfRemoteProtocols.TcpUdp"/>. Null means <see cref="ElympicsGameConfig.TcpPortForHalfRemoteMode"/>.</summary>
        public int? TcpUdpPort { get; }
        /// <summary>Used only when listening on <see cref="ElympicsHalfRemoteProtocols.WebRtc"/>. Null means <see cref="ElympicsGameConfig.WebPortForHalfRemoteMode"/>.</summary>
        public int? SignalingPort { get; }
        /// <summary>How many <see cref="ElympicsGameConfig.TestPlayers"/> entries the match uses. Null means all of them.</summary>
        public int? MaxPlayers { get; }

        public HalfRemoteServerLaunchData(ElympicsHalfRemoteProtocols? protocols = null, string? serverHostname = null, int? tcpUdpPort = null,
            int? signalingPort = null, int? maxPlayers = null)
        {
            Protocols = protocols;
            ServerHostname = serverHostname;
            TcpUdpPort = tcpUdpPort;
            SignalingPort = signalingPort;
            MaxPlayers = maxPlayers;
        }

        internal override string ModeName => "HalfRemoteServer";

        internal override bool TryValidate(ElympicsGameConfig gameConfig, ElympicsLaunchPlatform platform, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            if (!platform.CanListen)
                return Fail(ElympicsLaunchErrorCode.UnsupportedMode, $"{ModeName} is not supported: {ElympicsLaunchPlatform.CannotListenMessage}.", out failure);
            if (Protocols is { } protocols && (protocols == 0 || (protocols & ~ElympicsHalfRemoteProtocols.Both) != 0))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(Protocols)} must be {nameof(ElympicsHalfRemoteProtocols.TcpUdp)}, "
                    + $"{nameof(ElympicsHalfRemoteProtocols.WebRtc)} or {nameof(ElympicsHalfRemoteProtocols.Both)}, got {(int)protocols}.", out failure);
            if (ServerHostname != null && !IPAddress.TryParse(ServerHostname, out _))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(ServerHostname)} must be an IP address, got \"{ServerHostname}\".", out failure);
            if (TcpUdpPort is { } tcpUdpPort && !LaunchDataParsing.IsValidPort(tcpUdpPort))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(TcpUdpPort)} must be a port number, got {tcpUdpPort}.", out failure);
            if (SignalingPort is { } signalingPort && !LaunchDataParsing.IsValidPort(signalingPort))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(SignalingPort)} must be a port number, got {signalingPort}.", out failure);
            if (MaxPlayers is { } maxPlayers && (maxPlayers < 1 || maxPlayers > gameConfig.MaxPlayers))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload,
                    $"{nameof(MaxPlayers)} must be between 1 and {nameof(ElympicsGameConfig.MaxPlayers)} ({gameConfig.MaxPlayers}) of {gameConfig.GameName}, got {maxPlayers}.", out failure);
            return true;
        }

        private protected override GameSceneInitializer CreateInitializer(GameSceneLaunchContext context) => new HalfRemoteGameServerInitializer(this);
    }
}
