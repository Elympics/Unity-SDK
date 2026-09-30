using System;
using System.Collections.Specialized;
using System.Web;
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
            var uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);
            return query;
        }

        public static NameValueCollection GetUrlQuery() => GetQuery(GetHref());
        public static NameValueCollection GetElympicsUrlQuery() => GetQuery(GetElympicsHref());
    }
}
