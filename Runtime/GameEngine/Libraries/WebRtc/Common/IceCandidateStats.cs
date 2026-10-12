#nullable enable

using System;

namespace Elympics.GameEngine.Libraries.WebRtc
{
    [Serializable]
    internal struct IceCandidateStats
    {
        public string transportId;
        public string? address;
        public int port;
        public string protocol;
        public string candidateType;
        public int priority;
        public string? url;
        public string relayProtocol;
        public string foundation;
        public string relatedAddress;
        public int relatedPort;
        public string? usernameFragment;
        public string? tcpType;

        public bool HasTurnUrl() =>
            !string.IsNullOrEmpty(url) && (url.StartsWith("turn:") || url.StartsWith("turns:"));
    }
}
