using System.Collections.Generic;
using Elympics.Cysharp.Threading.Tasks;

namespace Elympics
{
    internal class DefaultRegionRetriever : IAvailableRegionRetriever
    {
        public async UniTask<List<string>> GetAvailableRegions() => await ElympicsRegions.GetAvailableRegions();
    }
}
