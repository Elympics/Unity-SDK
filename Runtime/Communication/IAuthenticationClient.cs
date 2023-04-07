using System;

namespace Elympics
{
	public interface IAuthenticationClient
	{
		void AuthenticateWithClientSecret(string endpoint, string clientSecret, Action<(bool Success, Guid UserId, string JwtToken, string Error)> callback);
	}
}
