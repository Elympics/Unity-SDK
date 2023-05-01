using System;
using Elympics.Models.Authentication;

namespace Elympics
{
	// TODO: in the process of renaming to shorter AuthData (backwards compatibility) ~dsygocki 2023-04-28
	public class AuthenticationData : AuthData
	{
		public AuthenticationData(AuthData authData)
			: base(authData.UserId, authData.JwtToken, authData.AuthType) { }
		public AuthenticationData(Guid userId, string jwtToken, AuthType authType = AuthType.None)
			: base(userId, jwtToken, authType) { }
		public AuthenticationData(AuthenticationDataResponse response, AuthType authType = AuthType.None)
			: base(response, authType) { }
	}
}
