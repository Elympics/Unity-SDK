using System;

namespace Elympics.Models.Authentication
{
	[Serializable]
	public class AuthenticationDataResponse
	{
		public string userId;
		public string jwtToken;
	}
}
