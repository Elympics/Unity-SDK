using Elympics.ElympicsSystems.Internal;
using WebRtcWrapper;

namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal static class WebRtcFactory
    {
        public static IWebRtcClient CreateClient(WebRtcConfig config, ElympicsLoggerContext logger) =>
            new WebRtcClient(config, logger);

        public static IWebRtcServer CreateServer(ElympicsLoggerContext logger) =>
            new WebRtcServer(logger);
    }
}
