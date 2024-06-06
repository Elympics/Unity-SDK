using Elympics.Models.Authentication;

namespace Elympics
{
    public struct ConnectionData
    {
        /// <summary>
        /// Type of authentication to be performed
        /// </summary>
        public AuthType? AuthType;
        /// <summary>
        /// If not provided, <see cref="ElympicsLobbyClient.CurrentRegion"/> will be used.
        /// </summary>
        public RegionData? Region;
        /// <summary>
        /// If not null, Elympics will try to connect using cached data.
        /// </summary>
        public CachedAuthData? AuthFromCacheData;

    }
}
