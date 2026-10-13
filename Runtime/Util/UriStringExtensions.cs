using System;
using System.Collections.Generic;
using System.Linq;

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
            var url = QueryString.Append(uriBuilder.Uri.ToString(), new[] { new KeyValuePair<string, string>(JwtProtocolAndQueryParameter, jwtToken) });
            return (url, JwtProtocolAndQueryParameter);
        }
    }
}
