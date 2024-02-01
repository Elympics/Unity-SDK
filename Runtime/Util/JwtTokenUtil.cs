using System;
using System.Text;
using UnityEngine;

namespace Elympics
{
    internal static class JwtTokenUtil
    {
        public static bool IsJwtExpired(string cachedDataJwtToken)
        {
            var split = cachedDataJwtToken.Split('.');
            if (split.Length != 3)
                throw new ArgumentException("Token must consist from 3 delimited by dot parts");

            var payloadData = split[1].Base64UrlDecode();
            var data = Encoding.UTF8.GetString(payloadData);
            var payload = JsonUtility.FromJson<JwtTokenPayload>(data);
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.exp;
        }
    }
}


