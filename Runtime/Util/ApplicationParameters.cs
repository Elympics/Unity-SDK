using System;
using System.Collections.Specialized;
using System.Globalization;

namespace Elympics
{
	public static partial class ApplicationParameters
	{
		private static NameValueCollection _urlQuery;
		public static  NameValueCollection UrlQuery => _urlQuery ?? (_urlQuery = ElympicsWebGL.GetUrlQuery());

		public static T GetParameter<T>(T defaultValue, string environmentKey, int argsIndex, string queryParameter)
		{
			string value = null;
			if (TryGetFromEnvironmentVariable(environmentKey, out var environmentValue))
				value = environmentValue;
			if (TryGetFromCommandLineArguments(argsIndex, out var argsValue))
				value = argsValue;
			if (TryGetFromURLQuery(UrlQuery, queryParameter, out var queryValue))
				value = queryValue;

			return string.IsNullOrEmpty(value)
				? defaultValue
				: (T) Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
		}

		private static bool TryGetFromEnvironmentVariable(string key, out string value)
		{
			value = default;
#if !UNITY_EDITOR
			if (!IsEnvironmentVariableDefined(key))
				return false;
			value = Environment.GetEnvironmentVariable(key);
			return true;
#else
			return false;
#endif
		}

		private static bool TryGetFromCommandLineArguments(int index, out string value)
		{
			value = default;
#if !UNITY_EDITOR && UNITY_STANDALONE
			var args = Environment.GetCommandLineArgs();
			if (args.Length <= index)
				return false;
			value = args[1];
			return true;
#else
			return false;
#endif
		}

		private static bool TryGetFromURLQuery(NameValueCollection query, string key, out string value)
		{
			value = default;
			if (query == null)
				return false;
			value = query[key];
			return !string.IsNullOrEmpty(value);
		}
		
		private static bool IsUnityServer()
		{
#if UNITY_SERVER
			return true;
#else
			return false;
#endif
		}

		private static bool IsUnityEditor()
		{
#if UNITY_EDITOR
			return true;
#else
			return false;
#endif
		}

		public static bool IsEnvironmentVariableDefined(string environmentKey) => Environment.GetEnvironmentVariables().Contains(environmentKey);
	}
}
