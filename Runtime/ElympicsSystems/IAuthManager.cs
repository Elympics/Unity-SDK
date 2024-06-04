using System;
using Elympics.Models.Authentication;
using JetBrains.Annotations;

#nullable enable

namespace Elympics
{
    [PublicAPI]
    public interface IAuthManager
    {
        public event Action<AuthData>? AuthenticationSucceeded;
        public event Action<string>? AuthenticationFailed;

        public AuthData? AuthData { get; }
        public Guid? UserGuid { get; }
        public bool IsAuthenticated { get; }

        /// <summary>
        /// Performs authentication of specified type. Has to be run before joining an online match.
        /// </summary>
        /// <param name="authType">Type of authentication to be performed.</param>
        /// <param name="region"> Region to connect. If default, it will use last used region or default <see cref="ElympicsRegions.Warsaw"/></param>
        /// <param name="customRegion">If true, there will be no validation check if region is listed in <see cref="ElympicsRegions.AllAvailableRegions"/></param>
        void AuthenticateWith(AuthType authType, string? region, bool customRegion = false);

        /// <summary>
        /// Resets the authentication state.
        /// After running this method, you have to authenticate using <see cref="AuthenticateWith"/> before joining an online match.
        /// </summary>
        void SignOut();
    }
}
