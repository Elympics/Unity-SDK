#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Elympics
{
    /// <summary>Replacement for <c>System.Web.HttpUtility</c> query handling, which isn't available on CoreCLR.</summary>
    internal static class QueryString
    {
        /// <summary>Mirrors <c>HttpUtility.ParseQueryString</c>: a segment without <c>=</c> is stored under a <c>null</c> key.</summary>
        public static NameValueCollection Parse(string? query)
        {
            var result = new NameValueCollection();
            if (string.IsNullOrEmpty(query))
                return result;

            foreach (var segment in query!.TrimStart('?').Split('&'))
            {
                if (segment.Length == 0)
                    continue;
                var separator = segment.IndexOf('=');
                if (separator < 0)
                    result.Add(null, Decode(segment));
                else
                    result.Add(Decode(segment[..separator]), Decode(segment[(separator + 1)..]));
            }
            return result;
        }

        public static string Append(string url, IEnumerable<KeyValuePair<string, string>> values) =>
            Append(new UriBuilder(url), string.Join("&", values.Select(kv => EscapePair(kv.Key, kv.Value))));

        public static string Append(UriBuilder uriBuilder, string key, string value) => Append(uriBuilder, EscapePair(key, value));

        private static string Append(UriBuilder uriBuilder, string added)
        {
            if (added.Length == 0)
                return uriBuilder.Uri.AbsoluteUri;
            var existing = uriBuilder.Query.TrimStart('?');
            uriBuilder.Query = existing.Length == 0 ? added : $"{existing}&{added}";
            return uriBuilder.Uri.AbsoluteUri;
        }

        private static string EscapePair(string key, string value) => $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";

        private static string Decode(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));
    }
}
