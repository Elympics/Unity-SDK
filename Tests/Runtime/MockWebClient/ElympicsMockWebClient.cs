using System;
using System.Collections.Generic;
using System.Threading;

namespace Elympics.Tests.MockWebClient
{
    internal class ElympicsMockWebClient : ElympicsWebClient
    {
        private readonly Dictionary<string, Func<WebRequestParams, object>> _handlers = new();

        protected override void SendRequest<T>(string method, string url, object jsonBody = null, string authorization = null,
            Action<Result<T, Exception>> callback = null, CancellationToken ct = default)
        {
            var reqParams = new WebRequestParams(method, url, jsonBody, authorization);

            foreach (var (pathEnding, handler) in _handlers)
                if (new UriBuilder(url).Path.TrimEnd().TrimEnd('/').EndsWith(pathEnding))
                {
                    Result<T, Exception> result;
                    try
                    {
                        result = Result<T, Exception>.Success((T)handler(reqParams));
                    }
                    catch (Exception e)
                    {
                        result = Result<T, Exception>.Failure(e);
                    }
                    callback?.Invoke(result);
                    return;
                }
        }

        public void AddHandler(string pathEnding, Func<WebRequestParams, object> handler) =>
            _handlers.Add(pathEnding, handler);
    }
}
