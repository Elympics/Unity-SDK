using Elympics.Models.Authentication;
using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    public struct CachedAuthData
    {
        public CachedAuthData(AuthData cachedData, bool autoRetryIfExpired)
        {
            CachedData = cachedData;
            AutoRetryIfExpired = autoRetryIfExpired;
        }

        public AuthData CachedData { get; init; }

        /// <summary>
        /// If true and JWT token is expired, performs call to Auth service using cached <see cref="AuthType"/>.
        /// </summary>
        public bool AutoRetryIfExpired { get; init; }
    }
}
