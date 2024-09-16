using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Elympics
{
    internal class DefaultRegionRetriever : IAvailableRegionRetriever
    {
        public async UniTask<List<string>> GetAvailableRegions() => await ElympicsRegions.GetAvailableRegions();
    }
}
