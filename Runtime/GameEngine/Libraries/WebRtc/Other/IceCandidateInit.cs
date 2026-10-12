#nullable enable
using System;
using Unity.WebRTC;

namespace Elympics.GameEngine.Libraries.WebRtc
{
    [Serializable]
    internal struct IceCandidateInitWithSdpMLineIndex
    {
        public string candidate;
        public string? sdpMid;
        public int sdpMLineIndex;
        public string? usernameFragment;

        public IceCandidateInitWithSdpMLineIndex(RTCIceCandidate iceCandidate)
        {
            candidate = iceCandidate.Candidate;
            sdpMid = iceCandidate.SdpMid;
            sdpMLineIndex = iceCandidate.SdpMLineIndex ?? throw new ArgumentNullException(nameof(iceCandidate.SdpMLineIndex));
            usernameFragment = iceCandidate.UserNameFragment;
        }
    }

    [Serializable]
    internal struct IceCandidateInitWithoutSdpMLineIndex
    {
        public string candidate;
        public string? sdpMid;
        public string? usernameFragment;

        public IceCandidateInitWithoutSdpMLineIndex(RTCIceCandidate iceCandidate)
        {
            candidate = iceCandidate.Candidate;
            sdpMid = iceCandidate.SdpMid;
            usernameFragment = iceCandidate.UserNameFragment;
        }
    }
}
