using System;
using UnityEngine.Networking;

namespace Elympics
{
	public class EditorEndpointChecker
	{
		private const string HealthQuery    = "health";
		private const int    DefaultTimeout = 3;

		private string _lastUrl;
		private Uri    _uri;
		private bool   _uriUpdated;

		private UnityWebRequest _request;

		public void UpdateUri(string url)
		{
			if (_lastUrl == url)
				return;
			_lastUrl = url;

			try
			{
				var validatedUrl = new Uri(url);
				var builder = new UriBuilder(validatedUrl);
				builder.Path = $"{builder.Path.TrimEnd('/')}/{HealthQuery}";
				builder.Query = "";
				builder.Fragment = "";
				_uri = builder.Uri;
				_uriUpdated = true;
			}
			catch (UriFormatException)
			{
				_uri = null;
			}
		}

		public void Update()
		{
			if (!_uriUpdated)
				return;

			_uriUpdated = false;
			_request?.Abort();
			_request = new UnityWebRequest(_uri)
			{
				timeout = DefaultTimeout
			};
			_request.SetTestCertificateHandlerIfNeeded();
			_request.SendWebRequest();
		}

		public bool IsUriCorrect        => _uri != null;
		public bool IsRequestDone       => _request?.isDone ?? false;
		public bool IsRequestSuccessful => !_request.IsProtocolError() && !_request.IsConnectionError();
	}
}
