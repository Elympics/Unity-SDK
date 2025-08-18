using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MatchTcpLibrary
{
    public interface IGameServerWebSignalingClient
    {
        UniTask<WebSignalingClientResponse> PostOfferAsync(string offer, int timeoutSeconds, CancellationToken ct = default);
        UniTask<WebSignalingClientResponse> OnIceCandidateCreated(string iceCandidate, int timeoutSeconds, string iceCandidateRoute, CancellationToken ct = default);
    }

    public class WebSignalingClientResponse
    {
        public bool IsError { get; set; }
        public string Text { get; set; }
    }
}
