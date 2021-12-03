using System;

namespace Elympics
{
	public interface IAuthenticationClient
	{
		void AuthenticateWithAuthTokenAsync(string endpoint, string authToken, Action<(bool Success, string UserId, string JwtToken, string Error)> callback);
	}
}
