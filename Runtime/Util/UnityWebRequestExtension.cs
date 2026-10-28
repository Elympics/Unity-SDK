using UnityEngine.Networking;
#if !(UNITY_WEBGL && !UNITY_EDITOR)
using Elympics.Core.Logger;
#endif

namespace Elympics
{
    public static class UnityWebRequestExtension
    {
        public static bool IsConnectionError(this UnityWebRequest request) =>
            request is { result: UnityWebRequest.Result.ConnectionError };

        public static bool IsProtocolError(this UnityWebRequest request) =>
            request is { result: UnityWebRequest.Result.ProtocolError };

        public static void SetTestCertificateHandlerIfNeeded(this UnityWebRequest request)
        {
            // The browser does TLS on WebGL, where a certificate handler has no effect but links X509 and Mono.Security.
#if !(UNITY_WEBGL && !UNITY_EDITOR)
            if (request.uri.Scheme != TestCertificateHandler.SecureScheme || !request.uri.Host.EndsWith(TestCertificateHandler.TestDomain))
                return;

            ElympicsLogger.LogInfo($"Test certificate handler set for domain: {request.uri.Host}");
            request.certificateHandler = new TestCertificateHandler();
#endif
        }

        public static void SetSdkVersionHeader(this UnityWebRequest request) =>
            request.SetRequestHeader("elympics-sdk-version", ElympicsConfig.SdkVersion);
    }
}
