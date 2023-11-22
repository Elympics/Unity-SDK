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
        void AuthenticateWith(AuthType authType);

        /// <summary>
        /// Resets the authentication state.
        /// After running this method, you have to authenticate using <see cref="AuthenticateWith"/> before joining an online match.
        /// </summary>
        void SignOut();
    }
}
