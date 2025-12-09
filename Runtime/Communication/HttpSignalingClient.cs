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

        private readonly Uri _signalingUri;

        public HttpSignalingClient(Uri baseUri, Guid matchId) =>
            _signalingUri = baseUri.AppendPathSegments(SignalingRoute, matchId.ToString());

        public async UniTask<WebSignalingClientResponse> PostOfferAsync(string offer, TimeSpan timeout, CancellationToken ct = default)
        {
            var rawOffer = Encoding.UTF8.GetBytes(offer);
            using var request = new UnityWebRequest(_signalingUri, UnityWebRequest.kHttpVerbPOST);
            request.timeout = (int)timeout.TotalSeconds;
            request.uploadHandler = new UploadHandlerRaw(rawOffer) { contentType = "application/json" };
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetTestCertificateHandlerIfNeeded();

            try
            {
                var (isCanceled, result) = await request.SendWebRequest().ToUniTask(null, PlayerLoopTiming.Update, ct).SuppressCancellationThrow();
                if (isCanceled)
                    return new WebSignalingClientResponse
                    {
                        IsError = true,
                        Text = "Operation cancelled.",
                        Code = 499
                    };
                return HandleCompleted(result);
            }
            catch (Exception e)
            {
                return new WebSignalingClientResponse
                {
                    IsError = true,
                    Text = e.Message,
                    Code = 500
                };
            }
        }

        public UniTask<WebSignalingClientResponse> OnIceCandidateCreated(string iceCandidate, TimeSpan timeout, string peerId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        private static WebSignalingClientResponse HandleCompleted(UnityWebRequest webRequest)
        {
            var isError = webRequest.IsConnectionError() || webRequest.IsProtocolError();
            var code = webRequest.responseCode;
            var text = webRequest.IsConnectionError()
                ? webRequest.error
                : webRequest.downloadHandler.text;
            return new WebSignalingClientResponse
            {
                IsError = isError,
                Text = text,
                Code = code
            };
        }
    }
}
