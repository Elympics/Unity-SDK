using System;
using System.Collections.Specialized;
using System.Web;
using Elympics.Core.Logger;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

#nullable enable

namespace Elympics
{
    internal static class ElympicsJsApi
    {
#if UNITY_WEBGL && !UNITY_EDITOR
		[DllImport("__Internal")] private static extern string ElympicsJsApi_GetHref();
		[DllImport("__Internal")] private static extern string ElympicsJsApi_GetElympicsHref();

        private static string GetHref() => ElympicsJsApi_GetHref();
        private static string GetElympicsHref() => ElympicsJsApi_GetElympicsHref();
#else
        private static string GetHref() => "";
        private static string GetElympicsHref() => "";
#endif

        private static NameValueCollection GetQuery(string url)
        {
            if (string.IsNullOrEmpty(url))
                return new NameValueCollection();
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                ElympicsLogger.LogWarning($"Ignoring the query of \"{url}\": not an absolute URL.");
                return new NameValueCollection();
            }
            return HttpUtility.ParseQueryString(uri.Query);
        }

        public static NameValueCollection GetUrlQuery() => GetQuery(GetHref());
        public static NameValueCollection GetElympicsUrlQuery() => GetQuery(GetElympicsHref());
    }
}
