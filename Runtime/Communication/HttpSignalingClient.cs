#nullable enable
using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using MatchTcpLibrary;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
    internal class HttpSignalingClient : IGameServerWebSignalingClient
    {
        private readonly Uri _uri;
        public HttpSignalingClient(Uri uri) => _uri = uri;

        public async UniTask<WebSignalingClientResponse> PostOfferAsync(string offer, TimeSpan timeout, CancellationToken ct = default)
        {
            var rawOffer = Encoding.UTF8.GetBytes(offer);
            using var request = new UnityWebRequest(_uri, UnityWebRequest.kHttpVerbPOST)
            {
                timeout = (int)timeout.TotalSeconds,
                uploadHandler = new UploadHandlerRaw(rawOffer) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer(),
            };
            request.SetTestCertificateHandlerIfNeeded();

            var (isCanceled, result) = await request.SendWebRequest().ToUniTask(null, PlayerLoopTiming.Update, ct).SuppressCancellationThrow();

            if (isCanceled)
                return new WebSignalingClientResponse
                {
                    IsError = true,
                    Text = "Operation Cancelled."
                };

            return HandleCompleted(result);
        }

        public async UniTask<WebSignalingClientResponse> OnIceCandidateCreated(string iceCandidate, TimeSpan timeout, string iceCandidateRoute, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(iceCandidateRoute))
            {
                throw new ElympicsException("No ice candidate route.");
            }
            var rawIceCandidate = Encoding.UTF8.GetBytes(iceCandidate);
            var uriBuilder = new UriBuilder(_uri);
            if (!uriBuilder.Path.EndsWith("/"))
                uriBuilder.Path += "/";
            uriBuilder.Path += iceCandidateRoute;

            using var request = new UnityWebRequest(uriBuilder.Uri, UnityWebRequest.kHttpVerbPOST);
            request.timeout = (int)timeout.TotalSeconds;
            request.uploadHandler = new UploadHandlerRaw(rawIceCandidate) { contentType = "application/json" };
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetTestCertificateHandlerIfNeeded();

            var (isCanceled, result) = await request.SendWebRequest().ToUniTask(null, PlayerLoopTiming.Update, ct).SuppressCancellationThrow();

            if (isCanceled)
                return new WebSignalingClientResponse()
                {
                    IsError = true,
                    Text = "Operation Cancelled."
                };

            return HandleCompleted(result);
        }


        private WebSignalingClientResponse HandleCompleted(UnityWebRequest webRequest)
        {
            var isError = webRequest.IsConnectionError() || webRequest.IsProtocolError();
            var text = webRequest.IsConnectionError()
                ? webRequest.error
                : webRequest.downloadHandler.text;
            return new WebSignalingClientResponse
            {
                IsError = isError,
                Text = text,
            };
        }
    }
}
