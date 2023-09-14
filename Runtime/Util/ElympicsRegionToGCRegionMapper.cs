using System.Collections.Generic;

namespace Elympics
{
    internal static class ElympicsRegionToGCRegionMapper
    {
        // source: https://github.com/GoogleCloudPlatform/gcping/blob/f4b288f7053a8080c454ef628b02f18c9462a8b9/internal/config/endpoints.go
        internal static readonly Dictionary<string, string> ElympicsRegionToGCRegionPingUrl = new()
        {
            // supported
            { ElympicsRegions.Warsaw, "https://europe-central2-5tkroniexa-lm.a.run.app" },
            { ElympicsRegions.Tokyo, "https://asia-northeast1-5tkroniexa-an.a.run.app" },
            { ElympicsRegions.Dallas, "https://us-south1-5tkroniexa-vp.a.run.app" },
            { ElympicsRegions.Mumbai, "https://asia-south1-5tkroniexa-el.a.run.app" },

            // not supported
            { ElympicsRegions.Taiwan, "https://asia-east1-5tkroniexa-de.a.run.app" },
            { ElympicsRegions.HongKong, "https://asia-east2-5tkroniexa-df.a.run.app" },
            { ElympicsRegions.Osaka, "https://asia-northeast2-5tkroniexa-dt.a.run.app" },
            { ElympicsRegions.Seoul, "https://asia-northeast3-5tkroniexa-du.a.run.app" },
            { ElympicsRegions.Delhi, "https://asia-south2-5tkroniexa-em.a.run.app" },
            { ElympicsRegions.Singapore, "https://asia-southeast1-5tkroniexa-as.a.run.app" },
            { ElympicsRegions.Jakarta, "https://asia-southeast2-5tkroniexa-et.a.run.app" },
            { ElympicsRegions.Sydney, "https://australia-southeast1-5tkroniexa-ts.a.run.app" },
            { ElympicsRegions.Melbourne, "https://australia-southeast2-5tkroniexa-km.a.run.app" },
            { ElympicsRegions.Finland, "https://europe-north1-5tkroniexa-lz.a.run.app" },
            { ElympicsRegions.Belgium, "https://europe-west1-5tkroniexa-ew.a.run.app" },
            { ElympicsRegions.London, "https://europe-west2-5tkroniexa-nw.a.run.app" },
            { ElympicsRegions.Frankfurt, "https://europe-west3-5tkroniexa-ey.a.run.app" },
            { ElympicsRegions.Netherlands, "https://europe-west4-5tkroniexa-ez.a.run.app" },
            { ElympicsRegions.Zurich, "https://europe-west6-5tkroniexa-oa.a.run.app" },
            { ElympicsRegions.Milan, "https://europe-west8-5tkroniexa-oc.a.run.app" },
            { ElympicsRegions.Paris, "https://europe-west9-5tkroniexa-od.a.run.app" },
            { ElympicsRegions.Madrid, "https://europe-southwest1-5tkroniexa-no.a.run.app" },
            { ElympicsRegions.TelAviv, "https://me-west1-5tkroniexa-zf.a.run.app/" },
            { ElympicsRegions.Montreal, "https://northamerica-northeast1-5tkroniexa-nn.a.run.app" },
            { ElympicsRegions.Toronto, "https://northamerica-northeast2-5tkroniexa-pd.a.run.app" },
            { ElympicsRegions.SaoPaulo, "https://southamerica-east1-5tkroniexa-rj.a.run.app" },
            { ElympicsRegions.Santiago, "https://southamerica-west1-5tkroniexa-tl.a.run.app" },
            { ElympicsRegions.Iowa, "https://us-central1-5tkroniexa-uc.a.run.app" },
            { ElympicsRegions.SouthCarolina, "https://us-east1-5tkroniexa-ue.a.run.app" },
            { ElympicsRegions.NorthVirginia, "https://us-east4-5tkroniexa-uk.a.run.app" },
            { ElympicsRegions.Columbus, "https://us-east5-5tkroniexa-ul.a.run.app" },
            { ElympicsRegions.Oregon, "https://us-west1-5tkroniexa-uw.a.run.app" },
            { ElympicsRegions.LosAngeles, "https://us-west2-5tkroniexa-wl.a.run.app" },
            { ElympicsRegions.SaltLakeCity, "https://us-west3-5tkroniexa-wm.a.run.app" },
            { ElympicsRegions.LasVegas, "https://us-west4-5tkroniexa-wn.a.run.app" },
        };
    }
}
