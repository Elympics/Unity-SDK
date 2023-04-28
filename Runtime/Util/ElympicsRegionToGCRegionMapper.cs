using System.Collections.Generic;

namespace Elympics
{
    internal static class ElympicsRegionToGCRegionMapper
    {
        // source: https://github.com/GoogleCloudPlatform/gcping/blob/f4b288f7053a8080c454ef628b02f18c9462a8b9/internal/config/endpoints.go
        internal static readonly Dictionary<string, string> ElympicsRegionToGCRegionPingUrl = new Dictionary<string, string>()
        {
            { ElympicsRegions.Tokyo, "https://asia-northeast1-5tkroniexa-an.a.run.app" },
            { ElympicsRegions.Warsaw, "https://europe-central2-5tkroniexa-lm.a.run.app" },
            { ElympicsRegions.TelAviv, "https://me-west1-5tkroniexa-zf.a.run.app" }
        };
    }
}
