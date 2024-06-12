using System;

namespace Elympics
{
    [Serializable]
    public class JwtTokenPayload
    {
        public string nameId;
        public string authType;
        public long nbf;
        public long exp;
        public long iat;
    }
}
