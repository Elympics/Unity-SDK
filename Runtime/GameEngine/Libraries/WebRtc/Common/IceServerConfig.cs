using System;

namespace Elympics
{
    [Serializable]
    internal struct IceServer
    {
        public string[] urls;
        public string username;
        public string credential;
    }

    [Serializable]
    internal struct IceServersResponse
    {
        public IceServer[] iceServers;
    }
}
