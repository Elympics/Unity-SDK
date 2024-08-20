namespace Elympics
{
    public class StandardRegionValidator : IRegionValidator
    {
        public bool IsRegionValid(RegionData regionData) => regionData.IsCustom || regionData.Name == string.Empty || ElympicsRegions.AllAvailableRegions.Contains(regionData.Name);
    }
}
