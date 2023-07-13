using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ElympicsApiModels.ApiModels.Games;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
    public static class ElympicsEditorWebClient
    {
        public static UnityWebRequestAsyncOperation SendEnginePostRequestApi(string url, string gameVersion, string[] filesPath, Action<UnityWebRequest> completed = null)
        {
            var formData = new List<IMultipartFormSection>();
            foreach (var path in filesPath)
                formData.Add(new MultipartFormFileSection(Routes.GamesUploadRequestFilesFieldName, File.ReadAllBytes(path), "pack.zip", "multipart/form-data"));
            formData.Add(new MultipartFormDataSection("gameVersion", gameVersion));
            var uri = new Uri(url);
            var request = UnityWebRequest.Post(uri, formData);
            request.SetRequestHeader("Authorization", $"Bearer {ElympicsConfig.AuthToken}");
            request.SetSdkVersionHeader();
            request.SetTestCertificateHandlerIfNeeded();

            var asyncOperation = request.SendWebRequest();
            AttachCompletedCallback(asyncOperation, completed);
            return asyncOperation;
        }

        public static UnityWebRequestAsyncOperation SendJsonGetRequestApi(string url, Action<UnityWebRequest> completed = null, bool silent = false)
        {
            var uri = new Uri(url);
            var request = UnityWebRequest.Get(uri);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {ElympicsConfig.AuthToken}");
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetSdkVersionHeader();
            request.SetTestCertificateHandlerIfNeeded();

#if ELYMPICS_DEBUG
            if (!silent)
                Debug.Log($"[Elympics] Sending request GET {url}");
#endif

            var asyncOperation = request.SendWebRequest();
            AttachCompletedCallback(asyncOperation, completed);
            return asyncOperation;
        }

        public static UnityWebRequestAsyncOperation SendJsonPostRequestApi(string url, object body, Action<UnityWebRequest> completed = null, bool auth = true)
        {
            var uri = new Uri(url);
            var request = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST);
            var bodyString = JsonUtility.ToJson(body);
            var bodyRaw = Encoding.ASCII.GetBytes(bodyString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            var token = ElympicsConfig.AuthToken;
            if (auth && !string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetSdkVersionHeader();
            request.SetTestCertificateHandlerIfNeeded();

#if ELYMPICS_DEBUG
            Debug.Log($"[Elympics] Sending request POST {url}\n{bodyString}");
#endif

            var asyncOperation = request.SendWebRequest();
            AttachCompletedCallback(asyncOperation, completed);
            return asyncOperation;
        }

        private static void AttachCompletedCallback(UnityWebRequestAsyncOperation asyncOperation, Action<UnityWebRequest> completed = null)
        {
            void RunCallback(UnityWebRequest request)
            {
                completed?.Invoke(request);
            }

            if (asyncOperation.isDone)
                RunCallback(asyncOperation.webRequest);
            else
                asyncOperation.completed += _ => RunCallback(asyncOperation.webRequest);
        }
    }
}
