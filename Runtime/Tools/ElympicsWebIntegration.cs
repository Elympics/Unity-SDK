#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Web;
using UnityEditor;
using UnityEngine;
using GamesRoutes = ElympicsApiModels.ApiModels.Games.Routes;
using AuthRoutes = ElympicsApiModels.ApiModels.Auth.Routes;
using Routes = ElympicsApiModels.ApiModels.Users.Routes;
using UnityEngine.Networking;

namespace Elympics
{
	public static class ElympicsWebIntegration
	{
		private const string PackFileName          = "pack.zip";
		private const string DirectoryNameToUpload = "Games";
		private const string EngineSubdirectory    = "Engine";
		private const string BotSubdirectory       = "Bot";

		private static string ElympicsWebEndpoint => ElympicsConfig.Load().ElympicsWebEndpoint;

		public static void Login()
		{
			Login(EditorPrefs.GetString(ElympicsConfig.UsernameKey), EditorPrefs.GetString(ElympicsConfig.PasswordKey));
		}

		public static void Logout()
		{
			EditorPrefs.SetBool(ElympicsConfig.IsLoginKey, false);
			EditorPrefs.SetString(ElympicsConfig.PasswordKey, string.Empty);
			EditorPrefs.SetString(ElympicsConfig.RefreshTokenKey, string.Empty);
			EditorPrefs.SetString(ElympicsConfig.AuthTokenKey, string.Empty);
			EditorPrefs.SetString(ElympicsConfig.UsernameKey, string.Empty);
		}

		public static bool IsConnectedToElympics()
		{
			if (!EditorPrefs.GetBool(ElympicsConfig.IsLoginKey))
			{
				Debug.LogError("Cannot connect with ElympicsWeb, check ElympicsWeb endpoint");
				return false;
			}

			return true;
		}

		[Serializable]
		private class LoginModel
		{
			public string UserName;
			public string Password;
		}

		[Serializable]
		private class CreateGameModel
		{
			public string GameName;
		}

		[Serializable]
		private class LoggedInTokenResponseModel
		{
			public string UserName;
			public string AuthToken;
			public string RefreshToken;
		}

		[Serializable]
		public class GameResponseModel
		{
			public string Id;
			public string Name;
		}

		private static UnityWebRequestAsyncOperation Login(string username, string password)
		{
			Debug.Log($"Logging in as {username} using password");

			var request = new LoginModel
			{
				UserName = username,
				Password = password
			};

			var uri = GetCombinedUrl(ElympicsWebEndpoint, AuthRoutes.BaseRoute, AuthRoutes.LoginRoute);
			var response = ElympicsWebClient.SendJsonPostRequestApi(uri, request);
			if (response.isDone)
				LoginHandler(response);
			else
				response.completed += _ => LoginHandler(response);

			return response;
		}

		private static void LoginHandler(UnityEngine.Networking.UnityWebRequestAsyncOperation response)
		{
			Debug.Log($"Login response code: {response.webRequest.responseCode}");
			if (ElympicsWebClient.TryDeserializeResponse(response, "Login", out LoggedInTokenResponseModel responseModel))
			{
				Debug.Log($"Logged to ElympicsWeb as {responseModel.UserName}");
				EditorPrefs.SetString(ElympicsConfig.AuthTokenKey, responseModel.AuthToken);
				EditorPrefs.SetString(ElympicsConfig.RefreshTokenKey, responseModel.RefreshToken);
				EditorPrefs.SetBool(ElympicsConfig.IsLoginKey, true);
			}
		}

		public static void GetElympicsEndpoint(Action<string> updateProperty)
		{
			var uri = GetCombinedUrl(ElympicsWebEndpoint, Routes.BaseRoute, Routes.EndpointRoutes);
			var response = ElympicsWebClient.SendJsonGetRequest(uri);

			response.completed += _ =>
			{
				if (ElympicsWebClient.TryDeserializeResponse(response, "Login", out string endpoint))
				{
					updateProperty.Invoke(endpoint);
					Debug.Log($"Set {endpoint} elympics endpoint");
				}
			};
		}

		public static void CreateGame(SerializedProperty gameName, SerializedProperty gameId)
		{
			var request = new CreateGameModel
			{
				GameName = gameName.stringValue
			};

			var url = GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute);
			var response = ElympicsWebClient.SendJsonPostRequestApi(url, request);

			response.completed += _ =>
			{
				if (ElympicsWebClient.TryDeserializeResponse(response, "Create game", out GameResponseModel responseModel))
				{
					gameId.SetValue(responseModel.Id);
					gameName.SetValue(responseModel.Name);
					Debug.Log($"Created game {responseModel.Name} with id {responseModel.Id}");
				}
			};
		}

		public static UnityWebRequestAsyncOperation UploadGame()
		{
			BuildTools.BuildElympicsServerLinux();

			try
			{
				var title = "Uploading to Elympics Cloud";
				EditorUtility.DisplayProgressBar(title, "", 0f);
				var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
				if (!EditorPrefs.GetBool(ElympicsConfig.IsLoginKey))
				{
					Debug.LogError("You must be logged in Elympics to upload games");
					return null;
				}

				EditorUtility.DisplayProgressBar(title, "Packing engine", 0.2f);
				if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.EnginePath, EngineSubdirectory, out string enginePath))
					return null;

				EditorUtility.DisplayProgressBar(title, "Packing bot", 0.4f);
				if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.BotPath, BotSubdirectory, out string botPath))
					return null;

				EditorUtility.DisplayProgressBar(title, "Uploading...", 0.8f);
				var url = GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute, currentGameConfig.GameId, GamesRoutes.GameVersionsRoute);
				var response = ElympicsWebClient.SendEnginePostRequest(url, currentGameConfig.GameVersion, new[] {enginePath, botPath});

				response.completed += _ => UploadHandler(currentGameConfig, response);

				EditorUtility.DisplayProgressBar(title, "Uploaded", 1f);
				return response;
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private static bool UploadHandler(ElympicsGameConfig currentGameConfig, UnityWebRequestAsyncOperation response)
		{
			if (response.webRequest.responseCode != 200)
			{
				ElympicsWebClient.LogResponseErrors("Upload game version", response.webRequest);
				return false;
			}

			Debug.Log($"Uploaded {currentGameConfig.GameName} with version {currentGameConfig.GameVersion}");
			return true;
		}

		public static void BuildAndUploadServerInBatchmode(string username, string password)
		{
			var gameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
			if (gameConfig == null)
				throw new ArgumentNullException("No elympics game config found. Configure your game first before trying to build a server.");
			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
				throw new ArgumentNullException($"Login credentials not found.");
			var operation = Login(username, password);
			while (!operation.isDone) ;
			LoginHandler(operation);
			Debug.Log("Login operation done");
			if (!EditorPrefs.GetBool(ElympicsConfig.IsLoginKey))
				throw new InvalidOperationException("Login operation failed. Check log for details");

			var uploadOperation = UploadGame();
			if (uploadOperation == null)
				throw new InvalidOperationException("Game upload didn't start. Check log for details");

			while (!uploadOperation.isDone) ;
			var ok = UploadHandler(gameConfig, uploadOperation);
			if (!ok)
				throw new InvalidOperationException("Game upload failed. Check log for details");
		}

		private static bool TryPack(string gameId, string gameVersion, string buildPath, string targetSubdirectory, out string destinationFilePath)
		{
			var destinationDirectoryPath = Path.Combine(DirectoryNameToUpload, gameId, gameVersion, targetSubdirectory);
			destinationFilePath = Path.Combine(destinationDirectoryPath, PackFileName);

			Directory.CreateDirectory(destinationDirectoryPath);
			try
			{
				Debug.Log($"Trying to pack {targetSubdirectory}");
				if (File.Exists(destinationFilePath))
					File.Delete(destinationFilePath);
				ZipFile.CreateFromDirectory(buildPath, destinationFilePath, System.IO.Compression.CompressionLevel.Optimal, false);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
				return false;
			}

			return true;
		}

		private static string AppendQueryParamsToUrl(string url, Dictionary<string, string> queryParamsDict)
		{
			var uriBuilder = new UriBuilder(url);
			var query = HttpUtility.ParseQueryString(uriBuilder.Query);
			foreach (var queryParam in queryParamsDict)
				query.Add(queryParam.Key, queryParam.Value);
			uriBuilder.Query = query.ToString();
			return uriBuilder.ToString();
		}

		private static string GetCombinedUrl(params string[] urlParts) => string.Join("/", urlParts);
	}
}
#endif
