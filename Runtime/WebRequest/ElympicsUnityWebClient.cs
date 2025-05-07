using System;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
    internal class ElympicsUnityWebClient : ElympicsWebClient
    {
        protected override void SendRequest<T>(
            string method,
            string url,
            object jsonBody = null,
            string authorization = null,
            Action<Result<T, Exception>> callback = null,
            CancellationToken ct = default)
        {
            var uri = new Uri(url);
            var request = new UnityWebRequest(uri, method)
            {
                downloadHandler = new DownloadHandlerBuffer()
            };

            var bodyString = string.Empty;
            if (jsonBody != null)
            {
                bodyString = SerializeJson(jsonBody);
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
            ElympicsLogger.Log($"Sending Web request: {method} {url}\n{body}");
#endif
            var asyncOperation = request.SendWebRequest();
            CallCallbackOnCompleted(asyncOperation, callback, ct);
        }

        private static T DeserializeJson<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }

        private static string SerializeJson<T>(T obj)
        {
            return JsonUtility.ToJson(obj);
        }

        private static void CallCallbackOnCompleted<T>(UnityWebRequestAsyncOperation requestOp, Action<Result<T, Exception>> callback, CancellationToken ct)
            where T : class
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
                ElympicsLogger.Log($"Received response {requestOp.webRequest.responseCode} "
                    + $"from {requestOp.webRequest.url}\n{requestOp.webRequest.downloadHandler.text}");
#endif
                if (requestOp.webRequest.responseCode != 200)
                {
                    RunCallback(Result<T, Exception>.Failure(
                        new ElympicsException($"{requestOp.webRequest.responseCode} - {requestOp.webRequest.error}\n{requestOp.webRequest.downloadHandler.text}")));
                    return;
                }

                // HACK: handling two cases, i.e. JSON-encoded string and plain text
                if (typeof(T) == typeof(string))
                {
                    var text = requestOp.webRequest.downloadHandler.text;
                    if (text[0] == '"' && text[^1] == '"' && text?.Count(x => x == '"') == 2)
                        text = text[1..^1];
                    RunCallback(Result<T, Exception>.Success((T)(object)text));
                    return;
                }

                T response;
                try
                {
                    response = DeserializeJson<T>(requestOp.webRequest.downloadHandler.text);
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
