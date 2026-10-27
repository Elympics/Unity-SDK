using System;
using System.Collections.Generic;
using System.Threading;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor.Communication;
using UnityEngine.Networking;

namespace Elympics.Editor.Tests
{
    internal sealed class FakeLoginStore : IEditorLoginStore
    {
        public bool IsLogin { get; set; } = true;
        public string AuthToken { get; set; } = "token";
        public string AuthTokenExp { get; set; } = long.MaxValue.ToString();
        public string RefreshToken { get; set; } = "refresh";
    }

    internal sealed class FakeTransport : IEditorApiTransport
    {
        public readonly List<EditorApiRequest> Requests = new();
        public Func<EditorApiRequest, EditorApiResponse> Respond = _ => new EditorApiResponse(200, "{}", null, UnityWebRequest.Result.Success);

        public UniTask<EditorApiResponse> SendAsync(EditorApiRequest request, CancellationToken ct)
        {
            Requests.Add(request);
            return UniTask.FromResult(Respond(request));
        }
    }

    internal sealed class EditorApiScope : IDisposable
    {
        private readonly IEditorApiTransport _transport = ElympicsEditorApi.Transport;
        private readonly IEditorLoginStore _login = ElympicsEditorApi.LoginStore;
        private readonly Func<long> _now = ElympicsEditorApi.GetUtcNowSeconds;
        private readonly Func<IEditorApiTransport> _createBlockingTransport = ElympicsEditorApi.CreateBlockingTransport;
        private readonly Func<string> _endpoint = ElympicsEditorApi.GetApiEndpointOverride;

        public EditorApiScope(FakeLoginStore login, FakeTransport transport, string apiEndpoint = "https://api.test/v2/api", long? now = null)
        {
            ElympicsEditorApi.Transport = transport;
            ElympicsEditorApi.CreateBlockingTransport = () => transport;
            ElympicsEditorApi.LoginStore = login;
            if (now.HasValue)
                ElympicsEditorApi.GetUtcNowSeconds = () => now.Value;
            ElympicsEditorApi.GetApiEndpointOverride = () => apiEndpoint;
        }

        public void Dispose()
        {
            ElympicsEditorApi.Transport = _transport;
            ElympicsEditorApi.LoginStore = _login;
            ElympicsEditorApi.GetUtcNowSeconds = _now;
            ElympicsEditorApi.CreateBlockingTransport = _createBlockingTransport;
            ElympicsEditorApi.GetApiEndpointOverride = _endpoint;
        }
    }
}
