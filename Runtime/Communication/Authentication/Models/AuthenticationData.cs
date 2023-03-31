using System;

namespace Elympics
{
	public class AuthenticationData
	{
		public Guid   UserId   { get; }
		public string JwtToken { get; }

		public AuthenticationData(Guid userId, string jwtToken)
		{
			UserId = userId;
			JwtToken = jwtToken;
		}
	}
}
