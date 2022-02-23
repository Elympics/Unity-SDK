using System;
using System.Collections;
using System.Text;
using MatchTcpLibrary;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
	public class HttpSignalingClient : IGameServerWebSignalingClient
	{
		private readonly Uri _uri;

		private const int RequestTimeout = 5;

		private UnityWebRequest _request;

		public HttpSignalingClient(Uri uri)
		{
			_uri = uri;
		}

		public IEnumerator PostOfferAsync(string offer)
		{
			_request?.Abort();

			var rawOffer = Encoding.UTF8.GetBytes(offer);

			_request = new UnityWebRequest(_uri, "POST")
			{
				timeout = RequestTimeout,
				uploadHandler = new UploadHandlerRaw(rawOffer) {contentType = "application/json"},
				downloadHandler = new DownloadHandlerBuffer()
			};
			
			ElympicsWebClient.AcceptTestCertificateHandler.SetOnRequestIfNeeded(_request);

			yield return _request.SendWebRequest();
		}

		public bool IsError => _request.isNetworkError || _request.isHttpError;

		public string Error
		{
			get
			{
				if (_request.isNetworkError)
					return _request.error;
				if (_request.isHttpError)
					return _request.downloadHandler.text;
				return null;
			}
		}

		public string Answer => !IsError ? _request.downloadHandler.text : null;
	}
}
