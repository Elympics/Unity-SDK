using Elympics.Models.Authentication;
using JetBrains.Annotations;

namespace Elympics
{
    public struct ConnectionData
    {
        /// <summary>
        /// Type of authentication to be performed
        /// </summary>
        [PublicAPI]
        public AuthType? AuthType;
        /// <summary>
        /// If not provided, <see cref="ElympicsLobbyClient.CurrentRegion"/> will be used.
        /// If this will be only data provided, Elympics will set new region and auto-rejoin lobby.
        /// </summary>
        [PublicAPI]
        public RegionData? Region;
        /// <summary>
        /// If not null, Elympics will try to connect using cached data.
        /// </summary>
        [PublicAPI]
        public CachedAuthData? AuthFromCacheData;

    }
}
