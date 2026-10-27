#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Elympics.Core.Logger;
using Elympics.Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Elympics.Editor.Communication
{
    internal sealed class EditorApiRequest
    {
        public string Method = UnityWebRequest.kHttpVerbGET;
        public string Url = null!;
        public string? JsonBody;
        public List<IMultipartFormSection>? Form;
        public byte[]? RawBody;
        public string? ContentType;
        public string? ContentEncoding;
        public bool ElympicsApi = true;
        public bool Auth = true;
        public int TimeoutSeconds = ElympicsEditorApi.JsonTimeoutSeconds;
    }

    internal readonly struct EditorApiResponse
    {
        public readonly long StatusCode;
        public readonly string? Text;
        public readonly string? Error;
        public readonly UnityWebRequest.Result Result;

        public EditorApiResponse(long statusCode, string? text, string? error, UnityWebRequest.Result result)
        {
            StatusCode = statusCode;
            Text = text;
            Error = error;
            Result = result;
        }

        public bool Succeeded => Result == UnityWebRequest.Result.Success;
    }

    internal interface IEditorApiTransport
    {
        UniTask<EditorApiResponse> SendAsync(EditorApiRequest request, CancellationToken ct);
    }

    internal sealed class UnityWebRequestEditorApiTransport : IEditorApiTransport
    {
        public async UniTask<EditorApiResponse> SendAsync(EditorApiRequest request, CancellationToken ct)
        {
            using var webRequest = Create(request);
            var operation = webRequest.SendWebRequest();
            if (!operation.isDone)
            {
                var completion = new UniTaskCompletionSource();
                operation.completed += _ => completion.TrySetResult();
                using (ct.Register(webRequest.Abort, useSynchronizationContext: true))
                    await completion.Task;
            }
            ct.ThrowIfCancellationRequested();

            return ReadResponse(webRequest);
        }

        internal static EditorApiResponse ReadResponse(UnityWebRequest webRequest) =>
            new(webRequest.responseCode, webRequest.downloadHandler?.text, webRequest.error, webRequest.result);

        internal static UnityWebRequest Create(EditorApiRequest request)
        {
            var uri = new Uri(request.Url);
            if (request.ElympicsApi)
                ElympicsApiHostPolicy.Require(uri);
            UnityWebRequest webRequest;
            if (request.Form != null)
                webRequest = UnityWebRequest.Post(uri, request.Form);
            else
            {
                webRequest = new UnityWebRequest(uri, request.Method) { downloadHandler = new DownloadHandlerBuffer() };
                var body = request.RawBody ?? (request.JsonBody != null ? Encoding.UTF8.GetBytes(request.JsonBody) : null);
                if (body != null)
                {
                    webRequest.uploadHandler = new UploadHandlerRaw(body);
                    webRequest.SetRequestHeader("Content-Type", request.ContentType ?? "application/json");
                }
            }

            if (!string.IsNullOrEmpty(request.ContentEncoding))
                webRequest.SetRequestHeader("Content-Encoding", request.ContentEncoding);
            if (request.TimeoutSeconds > 0)
                webRequest.timeout = request.TimeoutSeconds;

            if (request.ElympicsApi)
            {
                webRequest.SetRequestHeader("Accept", "application/json");
                var token = ElympicsEditorApi.LoginStore.AuthToken;
                if (request.Auth && !string.IsNullOrEmpty(token))
                    webRequest.SetRequestHeader("Authorization", $"Bearer {token}");
                webRequest.SetSdkVersionHeader();
                webRequest.SetTestCertificateHandlerIfNeeded();
                ElympicsLogger.LogDebug($"Sending Web request: {request.Method} {request.Url}");
            }
            return webRequest;
        }
    }

    /// <summary>Blocks the main thread; only for <see cref="ElympicsEditorApi.RunBlocking{T}"/>.</summary>
    internal sealed class BusyWaitEditorApiTransport : IEditorApiTransport
    {
        private const int PollIntervalMilliseconds = 5;

        public UniTask<EditorApiResponse> SendAsync(EditorApiRequest request, CancellationToken ct)
        {
            using var webRequest = UnityWebRequestEditorApiTransport.Create(request);
            ct.ThrowIfCancellationRequested();
            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
            {
                if (ct.IsCancellationRequested)
                {
                    webRequest.Abort();
                    ct.ThrowIfCancellationRequested();
                }
                Thread.Sleep(PollIntervalMilliseconds);
            }

            return UniTask.FromResult(UnityWebRequestEditorApiTransport.ReadResponse(webRequest));
        }
    }
}
