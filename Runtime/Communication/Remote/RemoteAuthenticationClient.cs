using System;
using LobbyPublicApiClients.Abstraction.User;
using LobbyPublicApiModels.User.Auth;

namespace Elympics
{
	public class RemoteAuthenticationClient : IAuthenticationClient
	{
		private readonly IUserApiClient _userApiClient;

		public RemoteAuthenticationClient(IUserApiClient userApiClient)
		{
			_userApiClient = userApiClient;
		}
		
		public void AuthenticateWithAuthTokenAsync(string endpoint, string authToken, Action<(bool Success, string UserId, string JwtToken, string Error)> callback)
		{
			_userApiClient.SetServerUri(endpoint);
			_userApiClient.SetAuthToken(authToken);
			_userApiClient.AuthenticateUserIdAsync(new AuthenticateUserIdModel.Request(), (response, exception) =>
			{
				if (exception != null)
				{
					callback((false, null, null, exception.Message));
					return;
				}

				callback((true, response.UserId, response.JwtToken, null));
			});
		}
	}
}
