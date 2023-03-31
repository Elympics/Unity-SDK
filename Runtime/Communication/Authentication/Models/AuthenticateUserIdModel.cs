using System;

namespace Elympics.Models.Authentication
{
	public static class AuthenticateUserIdModel
	{
		[Serializable]
		public class Request : ApiRequest
		{ }

		[Serializable]
		public class Response : ApiResponse
		{
			public string UserId;
			public string JwtToken;
		}
	}
}
