using WebRtcWrapper;

namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal static class WebRtcFactory
    {
        public static IWebRtcClient CreateClient(WebRtcConfig config) =>
            new WebRtcClient(config);

        public static IWebRtcServer CreateServer() =>
            new WebRtcServer();

        public static int LiveClientCount => WebRtcClient.InstanceCount;

        /// <summary>Peer connections held by the WebGL JS library, or -1 on other platforms.</summary>
        public static int JsInstanceCount => WebRtcClient.JsInstanceCount;

        /// <summary>Size of the WebGL wasm heap in bytes, or -1 on other platforms.</summary>
        public static long WasmHeapSize => WebRtcClient.WasmHeapSize;
    }
}
