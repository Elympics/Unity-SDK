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
			try
			{
				if (_lastUrl == url)
					return;

				_lastUrl = url;
				var builder = new UriBuilder(url);
				if (builder.Path != "/")
					throw new UriFormatException(builder.Path);

				builder.Path = HealthQuery;
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
			
			ElympicsWebClient.AcceptTestCertificateHandler.SetOnRequestIfNeeded(_request);

			_request.SendWebRequest();
		}

		public bool IsUriCorrect        => _uri != null;
		public bool IsRequestDone       => _request?.isDone ?? false;
		public bool IsRequestSuccessful => !(_request?.isHttpError | _request?.isNetworkError) ?? false;
	}
}
