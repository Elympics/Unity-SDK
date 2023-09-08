using System;
using System.Linq;

namespace Elympics
{
    internal static class StringExtensions
    {
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
    }
}
