using System;
using System.Threading;
using System.Web;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking.LongPolling;
using HybridWebSocket;
using AuthRoutes = Elympics.Models.Authentication.Routes;
using MatchmakingRoutes = Elympics.Models.Matchmaking.Routes;

namespace Elympics
{
	internal class UserApiClient : IUserApiClient
	{
		private const string JwtProtocolAndQueryParameter = "jwt_token";

		private string _serverUri;
		public string ServerUri
		{
			private get => _serverUri;
			set => _serverUri = new UriBuilder(value).ToString();
		}
		public string ClientSecret { private get; set; }

		public string JwtToken { get; set; }
		public string UserId { get; set; }

		public void AuthenticateWithClientSecret(AuthenticateUserIdModel.Request request, Action<AuthenticateUserIdModel.Response, Exception> callback, CancellationToken ct = default)
		{
			var url = GetUserUriWithBase(AuthRoutes.AuthenticateUserId);
			ElympicsWebClient.SendJsonPostRequest(url, request, ClientSecret, callback, ct);
		}

		public void JoinMatchmakerAndWaitForMatch(JoinMatchmakerAndWaitForMatchModel.Request request, Action<JoinMatchmakerAndWaitForMatchModel.Response, Exception> callback, CancellationToken ct = default)
		{
			var url = GetMatchmakingUriWithBase(MatchmakingRoutes.GetPendingMatchLongPolling);
			ElympicsWebClient.SendJsonPostRequest(url, request, $"Bearer {JwtToken}", callback, ct);
		}

		public void GetMatchLongPolling(GetMatchModel.Request request, Action<GetMatchModel.Response, Exception> callback, CancellationToken ct = default)
		{
			var url = GetMatchmakingUriWithBase(MatchmakingRoutes.GetMatchLongPolling);
			ElympicsWebClient.SendJsonPostRequest(url, request, $"Bearer {JwtToken}", callback, ct);
		}

		public IWebSocket CreateMatchmakingWebSocket()
		{
			return WebSocketFactory.CreateInstance(GetMatchmakingWsAddress(JwtToken), JwtProtocolAndQueryParameter);
		}

		private static string GetMatchmakingWsAddress(string jwtToken)
		{
			var uriBuilder = new UriBuilder(ElympicsConfig.Load().ElympicsLobbyEndpoint);
			uriBuilder.Path = string.Join("/", uriBuilder.Path.TrimEnd('/'), $"{MatchmakingRoutes.Base}/{MatchmakingRoutes.FindAndJoinMatch}");
			uriBuilder.Scheme = uriBuilder.Scheme == "https" ? "wss" : "ws";
			var query = HttpUtility.ParseQueryString(uriBuilder.Query);
			query.Add(JwtProtocolAndQueryParameter, jwtToken);
			uriBuilder.Query = query.ToString();
			return uriBuilder.Uri.ToString();
		}

		private string GetUserUriWithBase(string methodEndpoint)        => ConcatenatePathToLobbyUri($"{AuthRoutes.Base}/{methodEndpoint}");
		private string GetMatchmakingUriWithBase(string methodEndpoint) => ConcatenatePathToLobbyUri($"{MatchmakingRoutes.Base}/{methodEndpoint}");

		private string ConcatenatePathToLobbyUri(string path)
		{
			var uriBuilder = new UriBuilder(ServerUri);
			uriBuilder.Path = string.Join("/", uriBuilder.Path.TrimEnd('/'), path);
			return uriBuilder.Uri.ToString();
		}
	}
}
