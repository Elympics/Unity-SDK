using JetBrains.Annotations;
namespace Elympics
{
    [PublicAPI]
    public struct RegionData
    {
        public RegionData(string name, bool isCustom = false)
        {
            Name = name;
            IsCustom = isCustom;
        }
        /// <summary>
        /// Name of the region.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// If true, there will be no validation check if region is listed in <see cref="ElympicsRegions.AllAvailableRegions"/>
        /// </summary>
        public bool IsCustom { get; init; }
    }
}
