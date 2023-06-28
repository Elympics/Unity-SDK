namespace Elympics.Tests.MockWebClient
{
	internal class WebRequestParams
	{
		public string Method { get; }
		public string Url { get; }
		public object JsonBody { get; }
		public string Authorization { get; }

		public WebRequestParams(string method, string url, object jsonBody, string authorization)
		{
			Method = method;
			Url = url;
			JsonBody = jsonBody;
			Authorization = authorization;
		}
	}
}
