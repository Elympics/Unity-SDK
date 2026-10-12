#nullable enable

using System;
using System.Net;
using MatchTcpClients;

namespace Elympics
{
    internal static class LaunchDataParsing
    {
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

        public static bool IsValidPort(int port) => port is > IPEndPoint.MinPort and <= IPEndPoint.MaxPort;
    }
}
