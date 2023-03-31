using System;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
	public static class ElympicsWebClient
	{
		public static void SendJsonPostRequest<T>(string url, object body, string authorization = null, Action<T, Exception> callback = null, CancellationToken ct = default) where T : class
		{
			var uri = new Uri(url);
			var request = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST);
			var bodyString = JsonUtility.ToJson(body);
			var bodyRaw = Encoding.ASCII.GetBytes(bodyString);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();

			if (authorization != null)
				request.SetRequestHeader("Authorization", authorization);
			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("Accept", "application/json");
			request.SetSdkVersionHeader();
			request.SetTestCertificateHandlerIfNeeded();

#if ELYMPICS_DEBUG
			Debug.Log($"[Elympics] Sending request POST {url}\n{bodyString}");
#endif
			var asyncOperation = request.SendWebRequest();
			CallCallbackOnCompleted(asyncOperation, callback, ct);
		}

		private static void CallCallbackOnCompleted<T>(UnityWebRequestAsyncOperation requestOp, Action<T, Exception> callback, CancellationToken ct) where T : class
		{
			void RunCallback(T data, Exception exception)
			{
				callback?.Invoke(data, exception);
				requestOp.webRequest.Dispose();
			}

			var canceled = false;
			var ctRegistration = ct.Register(() =>
			{
				requestOp.webRequest.Abort();
				canceled = true;
			});
			requestOp.completed += _ =>
			{
				ctRegistration.Dispose();
				if (canceled)
				{
					RunCallback(null, new OperationCanceledException());
					return;
				}
#if ELYMPICS_DEBUG
				Debug.Log($"[Elympics] Received response {requestOp.webRequest.responseCode} from {requestOp.webRequest.url}\n{requestOp.webRequest.downloadHandler.text}");
#endif
				if (requestOp.webRequest.responseCode != 200)
				{
					RunCallback(null, new ElympicsException($"{requestOp.webRequest.responseCode} - {requestOp.webRequest.error}\n{requestOp.webRequest.downloadHandler.text}"));
					return;
				}

				T response;
				try
				{
					response = JsonUtility.FromJson<T>(requestOp.webRequest.downloadHandler.text);
				}
				catch (Exception e)
				{
					RunCallback(null, new ElympicsException($"{requestOp.webRequest.responseCode} - {e.Message}\n{requestOp.webRequest.downloadHandler.text}\n{e}"));
					return;
				}
				RunCallback(response, null);
			};
		}
	}
}
