using WebRtcWrapper;

namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal static class WebRtcFactory
    {
        public static IWebRtcClient CreateClient(WebRtcConfig config) =>
            new WebRtcClient(config);

        public static IWebRtcServer CreateServer() =>
            new WebRtcServer();
    }
}
