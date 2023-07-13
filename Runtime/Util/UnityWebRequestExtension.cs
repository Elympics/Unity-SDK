using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
    public static class UnityWebRequestExtension
    {
        public static bool IsConnectionError(this UnityWebRequest request)
        {
            return request != null && request.result == UnityWebRequest.Result.ConnectionError;
        }

        public static bool IsProtocolError(this UnityWebRequest request)
        {
            return request != null && request.result == UnityWebRequest.Result.ProtocolError;
        }

        public static void SetTestCertificateHandlerIfNeeded(this UnityWebRequest request)
        {
            if (request.uri.Scheme != TestCertificateHandler.SecureScheme || !request.uri.Host.EndsWith(TestCertificateHandler.TestDomain))
                return;

            Debug.Log($"Test domain cert handler set for domain {request.uri.Host}");
            request.certificateHandler = new TestCertificateHandler();
        }

        public static void SetSdkVersionHeader(this UnityWebRequest request)
        {
            request.SetRequestHeader("elympics-sdk-version", ElympicsConfig.SdkVersion);
        }
    }
}
