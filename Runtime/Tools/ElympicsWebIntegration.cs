#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
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

		private static string ElympicsWebEndpoint => ElympicsConfig.Load().ElympicsApiEndpoint;

		private static string RefreshEndpoint => GetCombinedUrl(ElympicsWebEndpoint, AuthRoutes.BaseRoute, AuthRoutes.RefreshRoute);

		public static void Login()
		{
			Login(ElympicsConfig.Username, ElympicsConfig.Password, LoginHandler);
		}

		public static void Logout()
		{
			SetAsLoggedOut();
			ElympicsConfig.Password = string.Empty;
			ElympicsConfig.RefreshToken = string.Empty;
			ElympicsConfig.AuthToken = string.Empty;
			ElympicsConfig.Username = string.Empty;
		}

		private static void SetAsLoggedOut() => ElympicsConfig.IsLogin = false;

		public static bool IsConnectedToElympics()
		{
			if (!ElympicsConfig.IsLogin)
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
		private class LoggedInTokenResponseModel
		{
			public string UserName     = null;
			public string AuthToken    = null;
			public string RefreshToken = null;
		}

		[Serializable]
		private class TokenRefreshingRequestModel
		{
			public string RefreshToken;
		}

		[Serializable]
		public class RefreshedTokensResponseModel
		{
			public string AuthToken;
			public string RefreshToken;
		}

		[Serializable]
		public class ElympicsEndpointsModel
		{
			public string Lobby;
			public string GameServers;
		}

		[Serializable]
		public class GameResponseModel
		{
			public string Id;
			public string Name;
		}

		[Serializable]
		public class RegionResponseModel
		{
			public string Id;
			public string Name;
			public string OrganizationId;
			public string Comment;
		}

		[Serializable]
		private class JwtMidPart
		{
			public long exp = 0;
		}

		private static UnityWebRequestAsyncOperation Login(string username, string password, Action<UnityWebRequest> completed = null)
		{
			Debug.Log($"Logging in as {username} using password");

			var model = new LoginModel
			{
				UserName = username,
				Password = password
			};

			var uri = GetCombinedUrl(ElympicsWebEndpoint, AuthRoutes.BaseRoute, AuthRoutes.LoginRoute);

			return ElympicsWebClient.SendJsonPostRequestApi(uri, model, completed, false);
		}

		private static void LoginHandler(UnityWebRequest webRequest)
		{
			Debug.Log($"Login response code: {webRequest.responseCode}");
			if (ElympicsWebClient.TryDeserializeResponse(webRequest, "Login", out LoggedInTokenResponseModel responseModel))
			{
				try
				{
					var authToken = responseModel.AuthToken;

					Debug.Log($"Logged to ElympicsWeb as {responseModel.UserName}");
					ElympicsConfig.AuthToken = authToken;
					ElympicsConfig.AuthTokenExp = GetAuthTokenMid(authToken).exp.ToString();
					ElympicsConfig.RefreshToken = responseModel.RefreshToken;
					ElympicsConfig.IsLogin = true;
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		private static JwtMidPart GetAuthTokenMid(string authToken)
		{
			var authTokenParts = authToken.Split('.');
			var authTokenMidPadded = authTokenParts[1].PadRight(4 * ((authTokenParts[1].Length + 3) / 4), '=');
			var authTokenMidStr = Encoding.ASCII.GetString(Convert.FromBase64String(authTokenMidPadded));
			var authTokenMid = JsonUtility.FromJson<JwtMidPart>(authTokenMidStr);
			return authTokenMid;
		}

		private const string RegionRoute = "regions/game";

		public static void GetAvailableRegionsForGameId(string gameId, Action<List<RegionResponseModel>> updateProperty, Action onFailure)
		{
			Debug.Log("Getting available regions");
			CheckAuthTokenAndRefreshIfNeeded(OnContinuation);

			void OnContinuation(bool success)
			{
				if (!success)
					return;

				var uri = GetCombinedUrl(ElympicsWebEndpoint, RegionRoute, gameId);
				ElympicsWebClient.SendJsonGetRequestApi(uri, OnCompleted);

				void OnCompleted(UnityWebRequest webRequest)
				{
					GetAvailableRegionsHandler(updateProperty, webRequest, onFailure);
				}
			}
		}

		public static void GetAvailableGames(Action<List<GameResponseModel>> updateProperty)
		{
			Debug.Log($"Getting available games");

			CheckAuthTokenAndRefreshIfNeeded(OnContinuation);

			void OnContinuation(bool success)
			{
				if (!success)
					return;

				var uri = GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute);
				ElympicsWebClient.SendJsonGetRequestApi(uri, OnCompleted);

				void OnCompleted(UnityWebRequest webRequest)
				{
					GetAvailableGamesHandler(updateProperty, webRequest);
				}
			}
		}

		private static void GetAvailableGamesHandler(Action<List<GameResponseModel>> updateProperty, UnityWebRequest webRequest)
		{
			Debug.Log($"Get available games response code: {webRequest.responseCode}");
			if (!ElympicsWebClient.TryDeserializeResponse(webRequest, "GetAvailableGames", out List<GameResponseModel> availableGames))
				return;

			updateProperty.Invoke(availableGames);
		}

		private static void GetAvailableRegionsHandler(Action<List<RegionResponseModel>> updateProperty, UnityWebRequest webRequest, Action onFailure)
		{
			Debug.Log($"Get available regions response code: {webRequest.responseCode}");
			if (ElympicsWebClient.TryDeserializeResponse(webRequest, "GetAvailableRegions", out List<RegionResponseModel> availableRegions))
			{
				updateProperty?.Invoke(availableRegions);
				return;
			}
			onFailure?.Invoke();
		}

		public static void GetElympicsEndpoints(Action<ElympicsEndpointsModel> updateProperty)
		{
			CheckAuthTokenAndRefreshIfNeeded(OnContinuation);

			void OnContinuation(bool success)
			{
				if (!success)
					return;

				var uri = GetCombinedUrl(ElympicsWebEndpoint, Routes.BaseRoute, Routes.EndpointRoutes);
				ElympicsWebClient.SendJsonGetRequestApi(uri, OnCompleted);

				void OnCompleted(UnityWebRequest webRequest)
				{
					if (ElympicsWebClient.TryDeserializeResponse(webRequest, "Get Elympics Endpoints", out ElympicsEndpointsModel endpoints))
					{
						updateProperty.Invoke(endpoints);
						Debug.Log($"Set {endpoints.Lobby} {endpoints.GameServers} elympics endpoints");
					}
				}
			}
		}

		public static void BuildAndUploadGame(Action<UnityWebRequest> completed = null)
		{
			CheckAuthTokenAndRefreshIfNeeded(OnContinuation);

			void OnContinuation(bool success)
			{
				const string title = "Uploading to Elympics Cloud";

				if (!success)
				{
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog(title, "Auth failed", "Check login state");
					return;
				}

				if (!BuildTools.BuildElympicsServerLinux())
					return;

				var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
				string enginePath;
				string botPath;
				var waitingForContinuation = false;
				try
				{
					EditorUtility.DisplayProgressBar(title, "", 0f);
					if (!ElympicsConfig.IsLogin)
					{
						Debug.LogError("You must be logged in Elympics to upload games");
						return;
					}

					EditorUtility.DisplayProgressBar(title, "Packing engine", 0.2f);
					if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.EnginePath, EngineSubdirectory, out enginePath))
						return;

					EditorUtility.DisplayProgressBar(title, "Packing bot", 0.4f);
					if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.BotPath, BotSubdirectory, out botPath))
						return;

					EditorUtility.DisplayProgressBar(title, "Uploading...", 0.8f);
					waitingForContinuation = true;
				}
				finally
				{
					if (!waitingForContinuation)
						EditorUtility.ClearProgressBar();
				}

				var url = GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute, currentGameConfig.GameId, GamesRoutes.GameVersionsRoute);
				ElympicsWebClient.SendEnginePostRequestApi(url, currentGameConfig.GameVersion, new[] { enginePath, botPath }, webRequest =>
				{
					try
					{
						HandleUploadResults(currentGameConfig, webRequest);
					}
					catch (Exception e)
					{
						EditorUtility.DisplayDialog(title, $"Upload failed: \n{e.Message}", "Ok");
					}

					EditorUtility.ClearProgressBar();
					completed?.Invoke(webRequest);
				});
			}
		}

		private static void HandleUploadResults(ElympicsGameConfig currentGameConfig, UnityWebRequest webRequest)
		{
			if (webRequest.IsProtocolError() || webRequest.IsConnectionError())
			{
				var errorMessage = ElympicsWebClient.ParseResponseErrors(webRequest);
				Debug.LogError($"Upload failed for game {currentGameConfig.GameName} with version: {currentGameConfig.GameVersion}\nGame ID: {currentGameConfig.GameId}");
				throw new ElympicsException(errorMessage);
			}

			Debug.Log($"Uploaded game {currentGameConfig.GameName} with version {currentGameConfig.GameVersion}");
		}

		public static void BuildAndUploadServerInBatchmode(string username, string password)
		{
			var gameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
			if (gameConfig == null)
				throw new ArgumentNullException("No elympics game config found. Configure your game first before trying to build a server.");
			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
				throw new ArgumentNullException($"Login credentials not found.");

			var loginOp = Login(username, password);

			while (!loginOp.isDone) ;

			LoginHandler(loginOp.webRequest);

			if (!ElympicsConfig.IsLogin)
				throw new Exception("Login operation failed. Check log for details");

			if (!BuildTools.BuildElympicsServerLinux())
				return;

			var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
			if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.EnginePath, EngineSubdirectory, out var enginePath))
				throw new Exception("Problem with packing engine");

			if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.BotPath, BotSubdirectory, out var botPath))
				throw new Exception("Problem with packing bot");

			var url = GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute, currentGameConfig.GameId, GamesRoutes.GameVersionsRoute);
			var uploadOp = ElympicsWebClient.SendEnginePostRequestApi(url, currentGameConfig.GameVersion, new[] { enginePath, botPath });

			while (!uploadOp.isDone) ;

			HandleUploadResults(currentGameConfig, uploadOp.webRequest);
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

		private static void CheckAuthTokenAndRefreshIfNeeded(Action<bool> continuation)
		{
			var authTokenExpStr = ElympicsConfig.AuthTokenExp;
			if (string.IsNullOrEmpty(authTokenExpStr))
			{
				SetAsLoggedOut();
				throw new ElympicsException("Can't check auth token expiration time. Are you logged in?");
			}

			var authTokenExp = long.Parse(authTokenExpStr);
			var currentTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
			if (currentTimestamp <= authTokenExp)
			{
				continuation?.Invoke(true);
				return;
			}

			Debug.Log("Auth token expired. Refreshing using refresh token...");
			var refreshToken = ElympicsConfig.RefreshToken;
			ElympicsWebClient.SendJsonPostRequestApi(RefreshEndpoint, new TokenRefreshingRequestModel { RefreshToken = refreshToken }, OnCompleted, false);

			void OnCompleted(UnityWebRequest webRequest)
			{
				var deserialized = ElympicsWebClient.TryDeserializeResponse(webRequest, "Refresh auth token", out RefreshedTokensResponseModel responseModel);
				if (deserialized)
				{
					var authToken = responseModel.AuthToken;
					ElympicsConfig.AuthToken = authToken;
					ElympicsConfig.AuthTokenExp = GetAuthTokenMid(authToken).exp.ToString();
					ElympicsConfig.RefreshToken = responseModel.RefreshToken;
				}
				else
				{
					Debug.LogError("Can't deserialize auth token");
					SetAsLoggedOut();
				}

				continuation?.Invoke(deserialized);
			}
		}

		private static string GetCombinedUrl(params string[] urlParts) => string.Join("/", urlParts);
	}
}
#endif