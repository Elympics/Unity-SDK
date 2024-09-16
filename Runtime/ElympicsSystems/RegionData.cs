using System;
using JetBrains.Annotations;
namespace Elympics
{
    [PublicAPI]
    public struct RegionData : IEquatable<RegionData>
    {

        [Obsolete("Use RegionData with region name only.")]
        public RegionData(string name, bool isCustom = false)
        {
            Name = name;
            IsCustom = isCustom;
        }

        public RegionData(string name)
        {
            Name = name;
#pragma warning disable CS0618 // Type or member is obsolete
            IsCustom = false;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Name of the region.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// If true, there will be no validation check if region is listed in <see cref="ElympicsRegions.AllAvailableRegions"/>
        /// </summary>
        [Obsolete("This parameter is outdated.")]
        public bool IsCustom { get; init; }

        public bool Equals(RegionData other) => Name == other.Name;
    }
}
