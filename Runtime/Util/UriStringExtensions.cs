using System;
using System.Linq;
using System.Web;

namespace Elympics
{
    internal static class StringExtensions
    {
        public const string JwtProtocolAndQueryParameter = "jwt_token";

        public static Uri AppendPathSegments(this string uri, params string[] segments)
        {
            var uriBuilder = new UriBuilder(uri);
            var oldPath = uriBuilder.Path.TrimEnd('/');
            uriBuilder.Path = string.Join('/', segments.Prepend(oldPath));
            return uriBuilder.Uri;
        }

        public static Uri AppendPathSegments(this Uri uri, params string[] segments)
        {
            var uriBuilder = new UriBuilder(uri);
            var oldPath = uriBuilder.Path.TrimEnd('/');
            uriBuilder.Path = string.Join('/', segments.Prepend(oldPath));
            return uriBuilder.Uri;
        }

        public static Uri AppendPathSegments(this string uri, string segment)
        {
            var uriBuilder = new UriBuilder(uri);
            var oldPath = uriBuilder.Path.TrimEnd('/');
            uriBuilder.Path = string.Join('/', oldPath, segment);
            return uriBuilder.Uri;
        }

        public static Uri AppendPathSegments(this Uri uri, string segment)
        {
            var uriBuilder = new UriBuilder(uri);
            var oldPath = uriBuilder.Path.TrimEnd('/');
            uriBuilder.Path = string.Join('/', oldPath, segment);
            return uriBuilder.Uri;
        }

        public static string GetAbsoluteOrRelativeString(this Uri uri) => uri.IsAbsoluteUri
            ? uri.AbsoluteUri
            : uri.OriginalString;

        public static (string Url, string Protocol) ToWebSocketAddress(this string uri, string jwtToken)
        {
            var uriBuilder = new UriBuilder(uri);
            uriBuilder.Scheme = uriBuilder.Scheme == "https" ? "wss" : "ws";
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query.Add(JwtProtocolAndQueryParameter, jwtToken);
            uriBuilder.Query = query.ToString();
            return (uriBuilder.Uri.ToString(), JwtProtocolAndQueryParameter);
        }
    }
}
