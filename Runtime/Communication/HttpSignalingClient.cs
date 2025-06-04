using System;
using System.Text;
using System.Threading;
using MatchTcpLibrary;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
    public class HttpSignalingClient : IGameServerWebSignalingClient
    {
        private readonly Uri _uri;

        private UnityWebRequestAsyncOperation _requestAsyncOperation;
        private CancellationTokenRegistration? _ctr;

        public event Action<WebSignalingClientResponse> ReceivedResponse;

        public HttpSignalingClient(Uri uri) =>
            _uri = uri;

        public void PostOfferAsync(string offer, int timeoutSeconds, CancellationToken ct = default)
        {
            Reset();
            var rawOffer = Encoding.UTF8.GetBytes(offer);
            var request = new UnityWebRequest(_uri, UnityWebRequest.kHttpVerbPOST)
            {
                timeout = timeoutSeconds,
                uploadHandler = new UploadHandlerRaw(rawOffer) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer(),
            };
            request.SetTestCertificateHandlerIfNeeded();

            _requestAsyncOperation = request.SendWebRequest();
            _requestAsyncOperation.completed += HandleCompleted;
            _ctr = ct.Register(ClearRequestData);
        }

        private void Reset()
        {
            _ctr?.Dispose();
            ClearRequestData();
        }

        private void ClearRequestData()
        {
            if (_requestAsyncOperation != null)
            {
                _requestAsyncOperation.completed -= HandleCompleted;
                _requestAsyncOperation.webRequest?.Abort();
                _requestAsyncOperation.webRequest?.Dispose();
            }
            _requestAsyncOperation = null;
        }

        private void HandleCompleted(AsyncOperation asyncOp)
        {
            asyncOp.completed -= HandleCompleted;
            if (asyncOp is not UnityWebRequestAsyncOperation webAsyncOp || webAsyncOp.webRequest == null)
                return;
            var request = webAsyncOp.webRequest;
            var isError = request.IsConnectionError() || request.IsProtocolError();
            var text = request.IsConnectionError()
                ? request.error
                : request.downloadHandler.text;
            Reset();
            ReceivedResponse?.Invoke(new WebSignalingClientResponse
            {
                IsError = isError,
                Text = text,
            });
        }
    }
}
