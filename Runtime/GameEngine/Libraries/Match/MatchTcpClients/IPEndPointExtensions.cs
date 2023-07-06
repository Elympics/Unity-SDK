using System;
using System.Net;

namespace MatchTcpClients
{
    public static class IPEndPointExtensions
    {
        public static IPEndPoint Parse(string endPoint)
        {
            var parts = endPoint.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException();

            return Parse(parts[0], int.Parse(parts[1]));
        }

        public static IPEndPoint Parse(string host, int port)
        {
            return new IPEndPoint(IPAddress.Parse(host), port);
        }
    }
}
