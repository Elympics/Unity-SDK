using System;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
	public static class ElympicsWebClient
	{
		public static void SendJsonPostRequest<T>(string url, object body, string authorization = null,
			Action<Result<T, Exception>> callback = null, CancellationToken ct = default) where T : class =>
			SendJsonRequest(UnityWebRequest.kHttpVerbPOST, url, body, authorization, callback, ct);

		public static void SendJsonPutRequest<T>(string url, object body, string authorization = null,
			Action<Result<T, Exception>> callback = null, CancellationToken ct = default) where T : class =>
			SendJsonRequest(UnityWebRequest.kHttpVerbPUT, url, body, authorization, callback, ct);

		private static void SendJsonRequest<T>(string method, string url, object body, string authorization = null, Action<Result<T, Exception>> callback = null, CancellationToken ct = default) where T : class
		{
			var uri = new Uri(url);
			var request = new UnityWebRequest(uri, method);
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

		private static void CallCallbackOnCompleted<T>(UnityWebRequestAsyncOperation requestOp, Action<Result<T, Exception>> callback, CancellationToken ct) where T : class
		{
			void RunCallback(Result<T, Exception> result)
			{
				callback?.Invoke(result);
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
					RunCallback(Result<T, Exception>.Failure(new OperationCanceledException()));
					return;
				}
#if ELYMPICS_DEBUG
				Debug.Log($"[Elympics] Received response {requestOp.webRequest.responseCode} from {requestOp.webRequest.url}\n{requestOp.webRequest.downloadHandler.text}");
#endif
				if (requestOp.webRequest.responseCode != 200)
				{
					RunCallback(Result<T, Exception>.Failure(new ElympicsException($"{requestOp.webRequest.responseCode} - {requestOp.webRequest.error}\n{requestOp.webRequest.downloadHandler.text}")));
					return;
				}

				// HACK: handling two cases, i.e. JSON-encoded string and plain text
				if (typeof(T) == typeof(string))
				{
					var text = requestOp.webRequest.downloadHandler.text;
					if (text?.Count(x => x == '"') == 2 && text[0] == '"' && text[text.Length - 1] == '"')
						text = text.Substring(1, text.Length - 2);
					RunCallback(Result<T, Exception>.Success((T)(object)text));
					return;
				}

				T response;
				try
				{
					response = JsonUtility.FromJson<T>(requestOp.webRequest.downloadHandler.text);
				}
				catch (Exception e)
				{
					RunCallback(Result<T, Exception>.Failure(new ElympicsException($"{requestOp.webRequest.responseCode} - {e.Message}\n{requestOp.webRequest.downloadHandler.text}\n{e}")));
					return;
				}
				RunCallback(Result<T, Exception>.Success(response));
			};
		}
	}
}
