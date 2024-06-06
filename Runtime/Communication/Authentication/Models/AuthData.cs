using System;

namespace Elympics.Models.Authentication
{
    public class AuthData
    {
        public Guid UserId { get; }
        public string JwtToken { get; }
        public AuthType AuthType { get; }
        public string Nickname { get; }
        internal string BearerAuthorization => $"Bearer {JwtToken}";

        public AuthData(Guid userId, string jwtToken, string nickname, AuthType authType = AuthType.None)
        {
            UserId = userId;
            JwtToken = jwtToken;
            AuthType = authType;
            Nickname = nickname;
        }

        public AuthData(AuthenticationDataResponse response, AuthType authType = AuthType.None)
        {
            UserId = new Guid(response.userId);
            JwtToken = response.jwtToken;
            AuthType = authType;
            Nickname = response.nickname;
        }
    }
}
