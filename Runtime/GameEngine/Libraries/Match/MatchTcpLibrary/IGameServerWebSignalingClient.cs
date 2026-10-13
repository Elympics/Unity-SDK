using System;
using System.Threading;
using Elympics;
using Elympics.Communication.Models;
using Elympics.Cysharp.Threading.Tasks;

namespace MatchTcpLibrary
{
    internal interface IGameServerWebSignalingClient
    {
        UniTask<IceServer[]> FetchIceServersAsync(TimeSpan timeout, CancellationToken ct = default);
        UniTask<SignalingResponse> PostOfferAsync(OfferWithCandidates offer, TimeSpan timeout, CancellationToken ct = default);
    }
}
