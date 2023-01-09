using System;
using System.Collections.Generic;
using System.IO;
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
			request.SetRequestHeader("elympics-sdk-version", ElympicsWebClient.SdkVersion);

			ElympicsWebClient.AcceptTestCertificateHandler.SetOnRequestIfNeeded(request);

			var asyncOperation = request.SendWebRequest();
			AttachCompletedCallback(asyncOperation, completed);
			return asyncOperation;
		}

		private static void AttachCompletedCallback(UnityWebRequestAsyncOperation asyncOperation, Action<UnityWebRequest> completed = null)
		{
			if (completed == null)
				return;
			if (asyncOperation.isDone)
				completed.Invoke(asyncOperation.webRequest);
			else
				asyncOperation.completed += _ => completed?.Invoke(asyncOperation.webRequest);
		}

		public static UnityWebRequestAsyncOperation SendJsonGetRequestApi(string url, Action<UnityWebRequest> completed = null)
		{
			var uri = new Uri(url);
			var request = UnityWebRequest.Get(uri);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Authorization", $"Bearer {ElympicsConfig.AuthToken}");
			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("Accept", "application/json");
			request.SetRequestHeader("elympics-sdk-version", ElympicsWebClient.SdkVersion);

			ElympicsWebClient.AcceptTestCertificateHandler.SetOnRequestIfNeeded(request);

			Debug.Log($"[Elympics] Sending request GET {url}");

			var asyncOperation = request.SendWebRequest();
			AttachCompletedCallback(asyncOperation, completed);
			return asyncOperation;
		}

		public static UnityWebRequestAsyncOperation SendJsonPostRequestApi(string url, object body, Action<UnityWebRequest> completed = null, bool auth = true)
		{
			var asyncOperation = ElympicsWebClient.SendJsonPostRequest(url, body, auth ? $"Bearer {ElympicsConfig.AuthToken}" : null);
			AttachCompletedCallback(asyncOperation, completed);
			return asyncOperation;
		}
	}
}
