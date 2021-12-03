using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ElympicsApiModels.ApiModels.Games;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Elympics
{
	public static class ElympicsWebClient
	{
#if UNITY_EDITOR
		public static UnityWebRequestAsyncOperation SendEnginePostRequest(string url, string gameVersion, string[] filesPath)
		{
			List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
			foreach (var path in filesPath)
				formData.Add(new MultipartFormFileSection(Routes.GamesUploadRequestFilesFieldName, File.ReadAllBytes(path), "pack.zip", "multipart/form-data"));
			formData.Add(new MultipartFormDataSection("gameVersion", gameVersion));
			UnityWebRequest request = UnityWebRequest.Post(url, formData);
			request.SetRequestHeader("Authorization", $"Bearer {EditorPrefs.GetString(ElympicsConfig.AuthTokenKey)}");
			return request.SendWebRequest();
		}

		public static UnityWebRequestAsyncOperation SendJsonGetRequest(string url)
		{
			UnityWebRequest request = UnityWebRequest.Get(url);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Authorization", $"Bearer {EditorPrefs.GetString(ElympicsConfig.AuthTokenKey)}");
			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("Accept", "application/json");
			return request.SendWebRequest();
		}

		public static UnityWebRequestAsyncOperation SendJsonPostRequestApi(string url, object body)
		{
			return SendJsonPostRequest(url, body, $"Bearer {EditorPrefs.GetString(ElympicsConfig.AuthTokenKey)}");
		}
#endif

		public static UnityWebRequestAsyncOperation SendJsonPostRequest(string url, object body, string auth)
		{
			var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
			var bodyString = JsonUtility.ToJson(body);
			var bodyRaw = Encoding.ASCII.GetBytes(bodyString);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Authorization", auth);
			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("Accept", "application/json");
			Debug.Log($"[Elympics] Sending request {url}\n{bodyString}");
			return request.SendWebRequest();
		}

		public static bool TryDeserializeResponse<T>(UnityWebRequestAsyncOperation response, string actionName, out T deserializedResponse)
		{
			deserializedResponse = default;
			if (response.webRequest.responseCode != 200)
			{
				LogResponseErrors(actionName, response.webRequest);
				return false;
			}

			if (typeof(T) == typeof(string))
			{
				deserializedResponse = (T)(object)response.webRequest.downloadHandler.text;
			}
			else
			{
				deserializedResponse = JsonUtility.FromJson<T>(response.webRequest.downloadHandler.text);
			}

			return true;
		}

		public static void LogResponseErrors(string actionName, UnityWebRequest request)
		{
			Debug.LogError("Received error in response for action " + actionName);
			
			if (request.responseCode == 403)
			{
				Debug.LogError("Requested resource is forbidden for this account");
				return;
			}

			if (request.responseCode == 401)
			{
				Debug.LogError("Unauthorized, please login to your ElympicsWeb accout");
				return;
			}

			ErrorModel errorModel = null;
			try
			{

				errorModel = JsonUtility.FromJson<ErrorModel>(request.downloadHandler.text);
			}
			catch (ArgumentException)
			{
			}
			
			if (errorModel?.Errors == null)
			{
				Debug.LogError($"Received error response code {request.responseCode} with error\n{request.downloadHandler.text}");
			}
			else
			{
				var errors = string.Join(", ", errorModel.Errors.SelectMany(r => r.Value.Select(x => x)));
				Debug.LogError($"Received error response code {request.responseCode} with errors\n{errors}");
			}
		}
		
		[Serializable]
		private class ErrorModel
		{
			public Dictionary<string, string[]> Errors;
		}
	}
}
