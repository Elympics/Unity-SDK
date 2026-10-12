#nullable enable
using System;
using Unity.WebRTC;

namespace Elympics.GameEngine.Libraries.WebRtc.Other
{
    [Serializable]
    internal struct SessionDescription
    {
        public string type;
        public string sdp;

        public SessionDescription(RTCSessionDescription sessionDescription)
        {
            type = sessionDescription.type.ToString().ToLower();
            sdp = sessionDescription.sdp;
        }

        public static explicit operator SessionDescription(RTCSessionDescription sessionDescription) => new(sessionDescription);

        public static explicit operator RTCSessionDescription(SessionDescription sessionDescription) => new()
        {
            type = sessionDescription.type switch
            {
                "offer" => RTCSdpType.Offer,
                "pranswer" => RTCSdpType.Pranswer,
                "answer" => RTCSdpType.Answer,
                "rollback" => RTCSdpType.Rollback,
                null => throw new ArgumentNullException(nameof(sessionDescription.type)),
                _ => throw new ArgumentOutOfRangeException(nameof(sessionDescription.type), sessionDescription.type, "Unsupported session description type"),
            },
            sdp = sessionDescription.sdp,
        };
    }
}
