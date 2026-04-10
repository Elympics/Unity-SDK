using System;

namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal struct WebRtcConfig
    {
        public static readonly WebRtcConfig Default = new()
        {
            OfferAnnounceDelay = TimeSpan.FromSeconds(1),
        };

        public TimeSpan OfferAnnounceDelay { get; set; }
    }
}
