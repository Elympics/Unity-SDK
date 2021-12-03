using System;
using System.Collections.Specialized;
using System.Web;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Elympics
{
	public static class ElympicsWebGL
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		[DllImport("__Internal")]
		public static extern string ElympicsGetHref();
#else
		public static string ElympicsGetHref() => null;
#endif

		public static NameValueCollection GetUrlQuery()
		{
			var href = ElympicsGetHref();
			if (string.IsNullOrEmpty(href))
				return new NameValueCollection();
			var uri = new Uri(href);
			var query = HttpUtility.ParseQueryString(uri.Query);
			return query;
		}
	}
}
