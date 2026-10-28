using System.Collections.Generic;
using Elympics.Cysharp.Threading.Tasks;

namespace Elympics
{
    internal interface IAvailableRegionRetriever
    {
        UniTask<List<string>> GetAvailableRegions();
    }
}
