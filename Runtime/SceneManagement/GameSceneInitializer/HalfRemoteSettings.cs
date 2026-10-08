#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Elympics
{
    /// <summary>
    /// What a half-remote client connects with: <see cref="HalfRemoteClientLaunchData"/> where set, otherwise
    /// the application parameters, otherwise <see cref="ElympicsGameConfig"/>.
    /// </summary>
    internal sealed class HalfRemoteClientSettings
    {
        public IPEndPoint ServerEndPoint { get; }
        public bool UseWeb { get; }
        public int PlayerIndex { get; }
        public string QueueName { get; }
        public string RegionName { get; }
        /// <summary>Of the local player. Null means the value of its test player entry.</summary>
        public byte[]? GameEngineData { get; }
        /// <summary>Of the local player. Null means the value of its test player entry.</summary>
        public float[]? MatchmakerData { get; }

        private HalfRemoteClientSettings(IPEndPoint serverEndPoint, bool useWeb, int playerIndex, string queueName, string regionName, byte[]? gameEngineData, float[]? matchmakerData)
        {
            ServerEndPoint = serverEndPoint;
            UseWeb = useWeb;
            PlayerIndex = playerIndex;
            QueueName = queueName;
            RegionName = regionName;
            GameEngineData = gameEngineData;
            MatchmakerData = matchmakerData;
        }

        public static HalfRemoteClientSettings Resolve(HalfRemoteClientLaunchData? data, ElympicsGameConfig config)
        {
            var useWeb = data?.UseTcpUdpInsteadOfWebRtc is { } useTcpUdp ? !useTcpUdp : config.UseWeb;
            IPEndPoint endPoint;
            if (data?.ServerAddress != null)
                endPoint = LaunchDataParsing.TryParseEndPoint(data.ServerAddress, out var parsed)
                    ? parsed!
                    : throw new ElympicsException($"Half-remote server address must be ip:port, got \"{data.ServerAddress}\".");
            else
            {
                // The port follows the transport in effect, which the launch data may have overridden.
                endPoint = new IPEndPoint(IPAddress.Parse(config.IpForHalfRemoteMode), config.GetPortForHalfRemoteMode(useWeb));
            }
            return new HalfRemoteClientSettings(endPoint, useWeb,
                data?.PlayerIndex ?? config.PlayerIndexForHalfRemoteMode,
                data?.QueueName ?? config.TestMatchData.queueName,
                data?.RegionName ?? config.TestMatchData.regionName,
                data?.GameEngineData?.ToArray(),
                data?.MatchmakerData?.ToArray());
        }
    }

    /// <summary>
    /// What a half-remote server listens on and how many players its match has: <see cref="HalfRemoteServerLaunchData"/> where set,
    /// otherwise <see cref="ElympicsGameConfig"/> (and its application parameters).
    /// </summary>
    internal sealed class HalfRemoteServerSettings
    {
        public IPEndPoint? TcpUdpEndPoint { get; }
        /// <summary>The signaling endpoint. Null if WebRTC is not listened on.</summary>
        public IPEndPoint? WebEndPoint { get; }
        /// <summary>How many <see cref="ElympicsGameConfig.TestPlayers"/> entries the match uses.</summary>
        public int PlayerCount { get; }
        /// <summary>Settings given for a protocol that is not listened on, and so ignored.</summary>
        public IReadOnlyList<string> Warnings { get; }

        private HalfRemoteServerSettings(IPEndPoint? tcpUdpEndPoint, IPEndPoint? webEndPoint, int playerCount, IReadOnlyList<string> warnings)
        {
            TcpUdpEndPoint = tcpUdpEndPoint;
            WebEndPoint = webEndPoint;
            PlayerCount = playerCount;
            Warnings = warnings;
        }

        public static HalfRemoteServerSettings Resolve(HalfRemoteServerLaunchData? data, ElympicsGameConfig config)
        {
            var protocols = data?.Protocols ?? ElympicsHalfRemoteProtocols.Both;
            var address = IPAddress.Parse(data?.ServerHostname ?? config.IpForHalfRemoteMode);
            var listensTcpUdp = (protocols & ElympicsHalfRemoteProtocols.TcpUdp) != 0;
            var listensWebRtc = (protocols & ElympicsHalfRemoteProtocols.WebRtc) != 0;
            if (!listensTcpUdp && !listensWebRtc)
                throw new ElympicsException("A half-remote server must listen on at least one protocol.");

            var warnings = new List<string>();
            if (!listensTcpUdp && data?.TcpUdpPort != null)
                warnings.Add($"{nameof(HalfRemoteServerLaunchData.TcpUdpPort)} {data.TcpUdpPort} is ignored, as the server does not listen on TCP/UDP.");
            if (!listensWebRtc && data?.SignalingPort != null)
                warnings.Add($"{nameof(HalfRemoteServerLaunchData.SignalingPort)} {data.SignalingPort} is ignored, as the server does not listen on WebRTC.");

            var tcpUdp = listensTcpUdp ? new IPEndPoint(address, data?.TcpUdpPort ?? config.TcpPortForHalfRemoteMode) : null;
            var web = listensWebRtc ? new IPEndPoint(address, data?.SignalingPort ?? config.WebPortForHalfRemoteMode) : null;
            var playerCount = Math.Min(data?.MaxPlayers ?? int.MaxValue, config.TestPlayers.Count);
            return new HalfRemoteServerSettings(tcpUdp, web, playerCount, warnings);
        }
    }
}
