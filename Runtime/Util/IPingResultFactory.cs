using Elympics.Cysharp.Threading.Tasks;

namespace Elympics
{
    internal interface IPingResultFactory
    {
        UniTask<PingResults> GetPingResult(string region);
    }
}
