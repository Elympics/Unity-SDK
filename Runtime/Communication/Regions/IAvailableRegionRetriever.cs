using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Elympics
{
    internal interface IAvailableRegionRetriever
    {
        UniTask<List<string>> GetAvailableRegions();
    }
}
