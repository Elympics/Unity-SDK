using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Models;
using Elympics.Core.Logger;
using Elympics.ElympicsSystems.Internal;
using MatchTcpClients;
using MatchTcpLibrary;
using UnityEngine;
using UnityEngine.Networking;

#nullable enable

namespace Elympics
{
    internal class HttpSignalingClient : IGameServerWebSignalingClient
    {
        private const string SignalingRoute = "doSignaling";
        private const string IceServersRoute = "iceServers";

        private readonly Uri _signalingUri;
        private readonly Uri _iceServersUri;
        private readonly ApplicationState _logger;
        private readonly GameServerClientConfig _config;

        public HttpSignalingClient(Uri baseUri, Guid matchId, GameServerClientConfig config)
        {
            _signalingUri = baseUri.AppendPathSegments(SignalingRoute, matchId.ToString());
            _iceServersUri = baseUri.AppendPathSegments(IceServersRoute, matchId.ToString());
            _logger = ElympicsLogger.CurrentContext.WithContext(nameof(HttpSignalingClient));
            _config = config;
        }

        public async UniTask<IceServer[]> FetchIceServersAsync(TimeSpan timeout, CancellationToken ct = default)
        {
            using var request = UnityWebRequest.Get(_iceServersUri);
            request.SetTestCertificateHandlerIfNeeded();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ = request.SendWebRequest();
            await UniTask.WaitUntil(() => request.isDone, cancellationToken: cts.Token).WithTimeout(timeout, cts.Token);
            cts.Cancel();
            if (!request.isDone)
                request.Abort();
            var response = HandleCompleted(request);
            if (response.IsError || string.IsNullOrEmpty(response.Text))
            {
                Debug.LogWarning($"[Elympics] Failed to fetch ICE servers from {_iceServersUri}: {response.Code} {response.Text ?? "cancelled"}. Proceeding without TURN.");
                return Array.Empty<IceServer>();
            }
            return JsonUtility.FromJson<IceServersResponse>(response.Text).iceServers ?? Array.Empty<IceServer>();
        }

        public async UniTask<SignalingResponse> PostOfferAsync(OfferWithCandidates offer, TimeSpan timeout, CancellationToken ct = default)
        {
            var logger = _logger.WithMethodName();
            var rawOffer = Encoding.UTF8.GetBytes(JsonUtility.ToJson(offer));
            using var request = new UnityWebRequest(_signalingUri, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(rawOffer) { contentType = "application/json" };
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetTestCertificateHandlerIfNeeded();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ = request.SendWebRequest();
            await UniTask.WaitUntil(() => request.isDone, cancellationToken: cts.Token).WithTimeout(timeout, cts.Token);
            cts.Cancel();
            if (!request.isDone)
                request.Abort();
            var response = HandleCompleted(request);
            if (response.Code == 502)
                throw new GameServerClosedException();
            if (response.IsError || string.IsNullOrEmpty(response.Text))
                throw logger.LogExceptionAndReturn(new ElympicsException($"No valid WebRTC answer has been received. Error: {response.Text} ({response.Code})"));

            return JsonUtility.FromJson<SignalingResponse>(response.Text);
        }

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

        private struct WebSignalingClientResponse
        {
            public bool IsError;
            public string? Text;
            public long Code;
        }
    }
}
