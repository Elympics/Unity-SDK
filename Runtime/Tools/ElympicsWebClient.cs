using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
    public static class ElympicsWebClient
	{
		public static void SendGetRequest<T>(string url, IEnumerable<KeyValuePair<string, string>> queryValues, string authorization = null,
			Action<Result<T, Exception>> callback = null, CancellationToken ct = default) where T : class =>
			SendRequest(UnityWebRequest.kHttpVerbGET, AddQueryParams(url, queryValues), null, authorization, callback, ct);

		public static void SendPostRequest<T>(string url, object jsonBody, string authorization = null,
			Action<Result<T, Exception>> callback = null, CancellationToken ct = default) where T : class =>
			SendRequest(UnityWebRequest.kHttpVerbPOST, url, jsonBody, authorization, callback, ct);

		public static void SendPutRequest<T>(string url, object jsonBody, string authorization = null,
			Action<Result<T, Exception>> callback = null, CancellationToken ct = default) where T : class =>
			SendRequest(UnityWebRequest.kHttpVerbPUT, url, jsonBody, authorization, callback, ct);

		private static void SendRequest<T>(string method, string url, object jsonBody = null, string authorization = null, Action<Result<T, Exception>> callback = null, CancellationToken ct = default) where T : class
		{
			var uri = new Uri(url);
			var request = new UnityWebRequest(uri, method);
			request.downloadHandler = new DownloadHandlerBuffer();

			string bodyString = string.Empty;
			if (jsonBody != null)
			{
				bodyString = JsonUtility.ToJson(jsonBody);
				var bodyRaw = Encoding.ASCII.GetBytes(bodyString);
				request.uploadHandler = new UploadHandlerRaw(bodyRaw);
				request.SetRequestHeader("Content-Type", "application/json");
			}

			if (authorization != null)
				request.SetRequestHeader("Authorization", authorization);
			request.SetRequestHeader("Accept", "application/json");
			request.SetSdkVersionHeader();
			request.SetTestCertificateHandlerIfNeeded();

#if ELYMPICS_DEBUG
			var body = string.IsNullOrEmpty(bodyString) ? "No request body." : $"{bodyString}";
			Debug.Log($"[Elympics] Sending request {method} {url}\n{body}");
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

		private static string AddQueryParams(string url, IEnumerable<KeyValuePair<string, string>> queryValues)
		{
			if (queryValues == null)
				return url;

			var uriBuilder = new UriBuilder(url);
			var query = HttpUtility.ParseQueryString(uriBuilder.Query);
			foreach (var (name, value) in queryValues)
				query.Add(name, value);
			uriBuilder.Query = query.ToString();
			return uriBuilder.Uri.ToString();
		}
	}
}
