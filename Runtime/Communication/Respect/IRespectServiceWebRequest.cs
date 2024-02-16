using System;
using Cysharp.Threading.Tasks;

namespace Elympics
{
    public interface IRespectServiceWebRequest
    {
        UniTask<GetRespectResponse> GetRespectForMatch(Guid matchId);
    }
}
