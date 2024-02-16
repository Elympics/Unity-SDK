using System;
using System.Collections.Generic;
using System.Threading;
using System.Web;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

#nullable enable

namespace Elympics
{
    internal abstract class ElympicsWebClient
    {
        private static ElympicsWebClient? instance;
        public static ElympicsWebClient Instance
        {
            get => instance ??= new ElympicsUnityWebClient();
            internal set => instance = value;
        }

        public static void SendGetRequest<T>(string url, IEnumerable<KeyValuePair<string, string>>? queryValues = null, string? authorization = null,
            Action<Result<T, Exception>>? callback = null, CancellationToken ct = default) where T : class =>
            Instance.SendRequest(UnityWebRequest.kHttpVerbGET, AddQueryParams(url, queryValues), null, authorization, callback, ct);

        public static void SendPostRequest<T>(string url, object? jsonBody = null, string? authorization = null,
            Action<Result<T, Exception>>? callback = null, CancellationToken ct = default) where T : class =>
            Instance.SendRequest(UnityWebRequest.kHttpVerbPOST, url, jsonBody, authorization, callback, ct);

        public static void SendPutRequest<T>(string url, object? jsonBody = null, string? authorization = null,
            Action<Result<T, Exception>>? callback = null, CancellationToken ct = default) where T : class =>
            Instance.SendRequest(UnityWebRequest.kHttpVerbPUT, url, jsonBody, authorization, callback, ct);

        protected abstract void SendRequest<T>(string method, string url, object? jsonBody = null,
            string? authorization = null, Action<Result<T, Exception>>? callback = null, CancellationToken ct = default)
            where T : class;

        private static string AddQueryParams(string url, IEnumerable<KeyValuePair<string, string>>? queryValues)
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

        public static Action<Result<TResult, Exception>> CreateResponseHandler<TResult>(UniTaskCompletionSource<TResult> tcs) =>
            result => _ = result.IsSuccess ? tcs.TrySetResult(result.Value) : tcs.TrySetException(result.Error);

        public static string CombinePath(UriBuilder uriBuilder, string initPath, string path)
        {
            uriBuilder.Path = string.Join("/", initPath, path);
            return uriBuilder.Uri.ToString();
        }
        public static string FillParams(string url, string separator, Dictionary<string, string> parameters)
        {
            var newUrl = url;
            foreach ((var key, var value) in parameters)
            {
                var replacedString = $"/{separator}{key}";
                if (!newUrl.Contains(replacedString))
                    throw new InvalidOperationException($"Invalid parameters in request: {url} | {key}");
                newUrl = newUrl.Replace(replacedString, $"/{value}");
            }
            return newUrl;
        }
    }
}
