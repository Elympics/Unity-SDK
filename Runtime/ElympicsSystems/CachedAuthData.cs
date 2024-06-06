using Elympics.Models.Authentication;

namespace Elympics
{
    public struct CachedAuthData
    {
        public AuthData CachedData { get; init; }

        /// <summary>
        /// If true, perform automatic call to Auth service using cached <see cref="AuthType"/>
        /// </summary>
        public bool AutoRetryIfExpired { get; init; }
    }
}
