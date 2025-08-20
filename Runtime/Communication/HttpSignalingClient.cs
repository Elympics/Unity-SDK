using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using MatchTcpLibrary;
using UnityEngine.Networking;

#nullable enable

namespace Elympics
{
    internal class HttpSignalingClient : IGameServerWebSignalingClient
    {
        private const string SignalingRoute = "doSignaling";
        private const string CandidateRoute = "addCandidate";

        private readonly Uri _signalingUri;
        private readonly Uri _baseCandidateUri;

        public HttpSignalingClient(Uri baseUri, Guid matchId)
        {
            _signalingUri = baseUri.AppendPathSegments(SignalingRoute, matchId.ToString());
            _baseCandidateUri = baseUri.AppendPathSegments(CandidateRoute, matchId.ToString());
        }

        public async UniTask<WebSignalingClientResponse> PostOfferAsync(string offer, TimeSpan timeout, CancellationToken ct = default)
        {
            var rawOffer = Encoding.UTF8.GetBytes(offer);
            using var request = new UnityWebRequest(_signalingUri, UnityWebRequest.kHttpVerbPOST);
            request.timeout = (int)timeout.TotalSeconds;
            request.uploadHandler = new UploadHandlerRaw(rawOffer) { contentType = "application/json" };
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetTestCertificateHandlerIfNeeded();

            var (isCanceled, result) = await request.SendWebRequest().ToUniTask(null, PlayerLoopTiming.Update, ct).SuppressCancellationThrow();

            if (isCanceled)
                return new WebSignalingClientResponse
                {
                    IsError = true,
                    Text = "Operation cancelled.",
                };

            return HandleCompleted(result);
        }

        public async UniTask<WebSignalingClientResponse> OnIceCandidateCreated(string iceCandidate, TimeSpan timeout, string peerId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(peerId))
                throw new ArgumentNullException(nameof(peerId));
            var rawIceCandidate = Encoding.UTF8.GetBytes(iceCandidate);
            var uri = _baseCandidateUri.AppendPathSegments(peerId);

            using var request = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST);
            request.timeout = (int)timeout.TotalSeconds;
            request.uploadHandler = new UploadHandlerRaw(rawIceCandidate) { contentType = "application/json" };
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetTestCertificateHandlerIfNeeded();

            var (isCanceled, result) = await request.SendWebRequest().ToUniTask(null, PlayerLoopTiming.Update, ct).SuppressCancellationThrow();

            if (isCanceled)
                return new WebSignalingClientResponse
                {
                    IsError = true,
                    Text = "Operation cancelled.",
                };

            return HandleCompleted(result);
        }


        private static WebSignalingClientResponse HandleCompleted(UnityWebRequest webRequest)
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
