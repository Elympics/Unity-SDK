using System;

namespace Elympics.Models.Authentication
{
    public class AuthData
    {
        public Guid UserId { get; }
        public string JwtToken { get; }
        public AuthType AuthType { get; }

        internal string BearerAuthorization => $"Bearer {JwtToken}";

        public AuthData(Guid userId, string jwtToken, AuthType authType = AuthType.None)
        {
            UserId = userId;
            JwtToken = jwtToken;
            AuthType = authType;
        }

        public AuthData(AuthenticationDataResponse response, AuthType authType = AuthType.None)
        {
            UserId = new Guid(response.userId);
            JwtToken = response.jwtToken;
            AuthType = authType;
        }
        private bool Equals(AuthData other) => UserId.Equals(other.UserId) && JwtToken == other.JwtToken && AuthType == other.AuthType;
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((AuthData)obj);
        }
        public override int GetHashCode() => HashCode.Combine(UserId, JwtToken, (int)AuthType);
    }
}
