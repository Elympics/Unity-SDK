using System;
using System.Collections.Specialized;
using System.Web;
using JetBrains.Annotations;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

#nullable enable

namespace Elympics
{
    internal static class ElympicsWebGL
    {
#if UNITY_WEBGL && !UNITY_EDITOR
		[DllImport("__Internal")] private static extern string ElympicsWebGL_GetHref();
		[DllImport("__Internal")] private static extern string ElympicsWebGL_GetElympicsHref();

        private static string GetHref() => ElympicsWebGL_GetHref();
        private static string GetElympicsHref() => ElympicsWebGL_GetElympicsHref();
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
