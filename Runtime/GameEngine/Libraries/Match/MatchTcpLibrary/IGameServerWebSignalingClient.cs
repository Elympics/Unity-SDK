using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MatchTcpLibrary
{
    internal interface IGameServerWebSignalingClient
    {
        UniTask<WebSignalingClientResponse> PostOfferAsync(string offer, TimeSpan timeout, CancellationToken ct = default);
        UniTask<WebSignalingClientResponse> OnIceCandidateCreated(string iceCandidate, TimeSpan timeout, string iceCandidateRoute, CancellationToken ct = default);
    }

    internal class WebSignalingClientResponse
    {
        public bool IsError { get; set; }
        public string Text { get; set; }
    }
}
