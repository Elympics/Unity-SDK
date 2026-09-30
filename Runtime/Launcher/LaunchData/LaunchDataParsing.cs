#nullable enable

using System;
using System.Net;
using MatchTcpClients;

namespace Elympics
{
    internal static class LaunchDataParsing
    {
        /// <summary>Parses <c>ip:port</c> as <see cref="IPEndPointExtensions.Parse(string)"/> does, without throwing.</summary>
        public static bool TryParseEndPoint(string? address, out IPEndPoint? endPoint)
        {
            endPoint = null;
            if (string.IsNullOrEmpty(address))
                return false;
            try
            {
                endPoint = IPEndPointExtensions.Parse(address!);
                return true;
            }
            catch (Exception e) when (e is ArgumentException or FormatException or OverflowException)
            {
                return false;
            }
        }

        /// <summary>False where <see cref="ElympicsGameConfig.UseWeb"/> is forced, as browsers have no TCP/UDP sockets.</summary>
        public static bool CanUseTcpUdp =>
#if UNITY_WEBGL
            false;
#else
            true;
#endif

        public const string NoTcpUdpMessage = "TCP/UDP is not available on WebGL. Use WebRTC.";

        public static bool IsValidPort(int port) => port is > IPEndPoint.MinPort and <= IPEndPoint.MaxPort;
    }
}
