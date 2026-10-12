using System;
using UnityEngine.Networking;

#nullable enable

namespace Elympics
{
    public class EditorEndpointChecker
    {
        private const string HealthQuery = "health";
        private const int DefaultTimeout = 3;

        private string? _lastUrl;
        private Uri? _uri;
        private bool _uriUpdated;

        private UnityWebRequest? _request;

        public void UpdateUri(string? url)
        {
            if (_lastUrl == url)
                return;
            _lastUrl = url;

            if (url is null || !Uri.TryCreate(url, UriKind.Absolute, out var validatedUrl))
            {
                _uri = null;
                return;
            }

            var builder = new UriBuilder(validatedUrl);
            builder.Path = $"{builder.Path.TrimEnd('/')}/{HealthQuery}";
            builder.Query = "";
            builder.Fragment = "";
            _uri = builder.Uri;
            _uriUpdated = true;
        }

        public void Update()
        {
            if (!_uriUpdated || _uri is null)
                return;

            _uriUpdated = false;
            _request?.Abort();
            _request?.Dispose();
            _request = new UnityWebRequest(_uri)
            {
                timeout = DefaultTimeout,
                downloadHandler = null,
                uploadHandler = null,
            };
            _request.SetTestCertificateHandlerIfNeeded();
            _ = _request.SendWebRequest();
        }

        public bool IsUriCorrect => _uri != null;
        public bool IsRequestDone => _request?.isDone ?? false;
        public bool IsRequestSuccessful => _request != null && !_request.IsProtocolError() && !_request.IsConnectionError();
    }
}
