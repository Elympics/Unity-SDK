using System;
using System.Linq;

namespace Elympics
{
    internal static class StringExtensions
    {
        public static string AppendPathSegments(this string uri, params string[] segments)
        {
            var uriBuilder = new UriBuilder(uri);
            var oldPath = uriBuilder.Path.TrimEnd('/');
            uriBuilder.Path = string.Join('/', segments.Prepend(oldPath));
            return uriBuilder.Uri.ToString();
        }

        public static string AppendPathSegments(this string uri, string segment)
        {
            var uriBuilder = new UriBuilder(uri);
            var oldPath = uriBuilder.Path.TrimEnd('/');
            uriBuilder.Path = string.Join('/', oldPath, segment);
            return uriBuilder.Uri.ToString();
        }
    }
}
