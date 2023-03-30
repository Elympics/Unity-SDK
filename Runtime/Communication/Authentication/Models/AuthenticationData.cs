using System;

namespace Elympics.Models.Authentication
{
	// TODO: rename to shorter AuthData (but uhh backwards compatibility) ~dsygocki 2023-04-19
	public class AuthenticationData
	{
		public Guid   UserId   { get; }
		public string JwtToken { get; }

		internal string BearerAuthorization => $"Bearer {JwtToken}";

		public AuthenticationData(Guid userId, string jwtToken)
		{
			UserId = userId;
			JwtToken = jwtToken;
		}

		public AuthenticationData(AuthenticationDataResponse response)
		{
			UserId = new Guid(response.userId);
			JwtToken = response.jwtToken;
		}
	}
}
