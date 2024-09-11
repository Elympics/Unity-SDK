using System.Collections.Generic;

namespace Elympics
{
    internal class StandardRegionValidator : IRegionValidator
    {
        public StandardRegionValidator(List<string> availableRegions) => GetAvailableRegions = availableRegions;
        public bool IsRegionValid(RegionData regionData) => regionData.Name == string.Empty || GetAvailableRegions.Contains(regionData.Name);
        public List<string> GetAvailableRegions { get; }
    }
}
