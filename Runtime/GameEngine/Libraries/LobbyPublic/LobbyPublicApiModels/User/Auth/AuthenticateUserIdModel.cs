using System;
using Daftmobile.Api;

namespace LobbyPublicApiModels.User.Auth
{
	public static class AuthenticateUserIdModel
	{
		public static class ErrorCodes
		{
			public const string MissingKeyForToken = "Can't generate JWT token, missing key.";
		}

		[Serializable]
		public class Request : ApiRequest
		{
			public override bool IsValid => true;
		}

		[Serializable]
		public class Response : ApiResponse
		{
			public string UserId;
			public string JwtToken;
		}
	}
}
