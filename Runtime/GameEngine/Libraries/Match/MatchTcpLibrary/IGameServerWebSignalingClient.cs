using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using Elympics.Communication.Models;

namespace MatchTcpLibrary
{
    internal interface IGameServerWebSignalingClient
    {
        UniTask<IceServer[]> FetchIceServersAsync(TimeSpan timeout, CancellationToken ct = default);
        UniTask<SignalingResponse> PostOfferAsync(OfferWithCandidates offer, TimeSpan timeout, CancellationToken ct = default);
    }
}
