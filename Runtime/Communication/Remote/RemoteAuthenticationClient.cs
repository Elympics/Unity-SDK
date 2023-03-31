using System;
using Elympics.Models.Authentication;

namespace Elympics
{
	internal class RemoteAuthenticationClient : IAuthenticationClient
	{
		private readonly IUserApiClient _userApiClient;

		internal RemoteAuthenticationClient(IUserApiClient userApiClient)
		{
			_userApiClient = userApiClient;
		}

		public void AuthenticateWithClientSecret(string endpoint, string clientSecret, Action<(bool Success, Guid UserId, string JwtToken, string Error)> callback)
		{
			_userApiClient.ServerUri = endpoint;
			_userApiClient.ClientSecret = clientSecret;
			_userApiClient.AuthenticateWithClientSecret(new AuthenticateUserIdModel.Request(), (response, exception) =>
			{
				if (exception != null)
				{
					callback((false, Guid.Empty, null, exception.Message));
					return;
				}

				_userApiClient.JwtToken = response.JwtToken;
				_userApiClient.UserId = response.UserId;
				callback((true, new Guid(response.UserId), response.JwtToken, null));
			});
		}
	}
}
