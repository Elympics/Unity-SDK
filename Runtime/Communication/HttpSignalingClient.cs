using System;
using System.Text;
using System.Threading;
using MatchTcpLibrary;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
	public class HttpSignalingClient : IGameServerWebSignalingClient
	{
		private readonly Uri _uri;

		private UnityWebRequestAsyncOperation _requestAsyncOperation;
		private CancellationTokenRegistration? _ctr;

		public event Action<IGameServerWebSignalingClient.Response> ReceivedResponse;

		public HttpSignalingClient(Uri uri)
		{
			_uri = uri;
		}

		public void PostOfferAsync(string offer, int timeoutSeconds, CancellationToken ct = default)
		{
			Reset();
			var rawOffer = Encoding.UTF8.GetBytes(offer);
			var request = new UnityWebRequest(_uri, UnityWebRequest.kHttpVerbPOST)
			{
				timeout = timeoutSeconds,
				uploadHandler = new UploadHandlerRaw(rawOffer) { contentType = "application/json" },
				downloadHandler = new DownloadHandlerBuffer()
			};

			ElympicsWebClient.AcceptTestCertificateHandler.SetOnRequestIfNeeded(request);

			_requestAsyncOperation = request.SendWebRequest();
			_requestAsyncOperation.completed += HandleCompleted;
			_ctr = ct.Register(Reset);
		}

		private void Reset()
		{
			if (_requestAsyncOperation != null)
			{
				_requestAsyncOperation.completed -= HandleCompleted;
				_requestAsyncOperation.webRequest?.Abort();
			}
			_requestAsyncOperation = null;
			_ctr?.Dispose();
		}

		private void HandleCompleted(AsyncOperation asyncOp)
		{
			asyncOp.completed -= HandleCompleted;
			if (!(asyncOp is UnityWebRequestAsyncOperation webAsyncOp) || webAsyncOp.webRequest == null)
				return;
			var request = webAsyncOp.webRequest;
			var isError = request.IsConnectionError() || request.IsProtocolError();
			var text = request.IsConnectionError()
				? request.error
				: request.downloadHandler.text;
			ReceivedResponse?.Invoke(new IGameServerWebSignalingClient.Response
			{
				IsError = isError,
				Text = text
			});
		}
	}
}
