using System.Collections.Generic;
namespace Elympics
{
    internal interface IRegionValidator
    {
        bool IsRegionValid(RegionData regionData);
        List<string> GetAvailableRegions { get; }
    }
}
