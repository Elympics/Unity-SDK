using System;
using System.Threading;
using System.Threading.Tasks;
using LobbyPublicApiClients.Abstraction.User;
using LobbyPublicApiModels.User.Auth;
using LobbyPublicApiModels.User.ClientProfiling;
using LobbyPublicApiModels.User.Matchmaking;
using UnityEngine;
using UnityEngine.Networking;
using AuthRoutes = LobbyPublicApiModels.User.Auth.Routes;
using MatchmakingRoutes = LobbyPublicApiModels.User.Matchmaking.Routes;
using ClientProfilingRoutes = LobbyPublicApiModels.User.ClientProfiling.Routes;

namespace Elympics
{
	public class UserApiClient : IUserApiClient
	{
		private string _lobbyUrl;
		private string _authToken;

		public void SetServerUri(string host, int port)
		{
			var builder = new UriBuilder(host) {Port = port};
			_lobbyUrl = builder.Uri.ToString();
		}

		public void SetServerUri(string address)
		{
			var builder = new UriBuilder(address);
			_lobbyUrl = builder.Uri.ToString();
		}

		public void SetAuthToken(string authToken)
		{
			_authToken = authToken;
		}

		public Task<AuthenticateUserIdModel.Response> AuthenticateUserIdAsync(AuthenticateUserIdModel.Request request, CancellationToken ct = default) => throw new NotImplementedException();

		public void AuthenticateUserIdAsync(AuthenticateUserIdModel.Request request, Action<AuthenticateUserIdModel.Response, Exception> callback, CancellationToken ct = default)
		{
			var requestOp = ElympicsWebClient.SendJsonPostRequest(GetUserUriWithBase(AuthRoutes.AuthenticateUserId), request, _authToken);
			CallCallbackOnCompleted(requestOp, callback, ct);
		}


		public Task<SetAttemptReferenceModel.Response> SetAttemptReferenceAsync(SetAttemptReferenceModel.Request request, CancellationToken ct = default) => throw new NotImplementedException();

		public void SetAttemptReferenceAsync(SetAttemptReferenceModel.Request request, Action<SetAttemptReferenceModel.Response, Exception> callback, CancellationToken ct = default)
		{
			var requestOp = ElympicsWebClient.SendJsonPostRequest(GetClientProfilingUriWithBase(ClientProfilingRoutes.SetAttemptReference), request, _authToken);
			CallCallbackOnCompleted(requestOp, callback, ct);
		}

		public Task<JoinMatchmakerAndWaitForMatchModel.Response> JoinMatchmakerAndWaitForMatchAsync(JoinMatchmakerAndWaitForMatchModel.Request request, CancellationToken ct = default) => throw new NotImplementedException();

		public void JoinMatchmakerAndWaitForMatchAsync(JoinMatchmakerAndWaitForMatchModel.Request request, Action<JoinMatchmakerAndWaitForMatchModel.Response, Exception> callback, CancellationToken ct = default)
		{
			var requestOp = ElympicsWebClient.SendJsonPostRequest(GetMatchmakingUriWithBase(MatchmakingRoutes.GetPendingMatchLongPolling), request, _authToken);
			CallCallbackOnCompleted(requestOp, callback, ct);
		}


		public Task<GetMatchModel.Response> GetMatchLongPollingAsync(GetMatchModel.Request request, CancellationToken ct = default) => throw new NotImplementedException();

		public void GetMatchLongPollingAsync(GetMatchModel.Request request, Action<GetMatchModel.Response, Exception> callback, CancellationToken ct = default)
		{
			var requestOp = ElympicsWebClient.SendJsonPostRequest(GetMatchmakingUriWithBase(MatchmakingRoutes.GetMatchLongPolling), request, _authToken);
			CallCallbackOnCompleted(requestOp, callback, ct);
		}


		public Task<GetUnfinishedMatchesModel.Response> GetUnfinishedMatchesAsync(GetUnfinishedMatchesModel.Request request, CancellationToken ct = default) => throw new NotImplementedException();

		public void GetUnfinishedMatchesAsync(GetUnfinishedMatchesModel.Request request, Action<GetUnfinishedMatchesModel.Response, Exception> callback, CancellationToken ct = default)
		{
			var requestOp = ElympicsWebClient.SendJsonPostRequest(GetMatchmakingUriWithBase(MatchmakingRoutes.GetUnfinishedMatches), request, _authToken);
			CallCallbackOnCompleted(requestOp, callback, ct);
		}

		private string GetUserUriWithBase(string methodEndpoint)            => new Uri(new Uri(_lobbyUrl), $"{AuthRoutes.Base}/{methodEndpoint}").ToString();
		private string GetMatchmakingUriWithBase(string methodEndpoint)     => new Uri(new Uri(_lobbyUrl), $"{MatchmakingRoutes.Base}/{methodEndpoint}").ToString();
		private string GetClientProfilingUriWithBase(string methodEndpoint) => new Uri(new Uri(_lobbyUrl), $"{ClientProfilingRoutes.Base}/{methodEndpoint}").ToString();


		private static void CallCallbackOnCompleted<T>(UnityWebRequestAsyncOperation requestOp, Action<T, Exception> callback, CancellationToken ct)
			where T : class
		{
			var ctRegistration = ct.Register(() =>
			{
				requestOp.webRequest.Abort();
				callback(null, null);
			});
			requestOp.completed += _ =>
			{
				ctRegistration.Dispose();
				if (requestOp.webRequest.responseCode != 200)
				{
					callback(null, new Exception($"[Elympics] {requestOp.webRequest.responseCode} - {requestOp.webRequest.error}\n{requestOp.webRequest.downloadHandler.text}"));
					return;
				}

				var response = JsonUtility.FromJson<T>(requestOp.webRequest.downloadHandler.text);
				Debug.Log($"[Elympics] Received response from {requestOp.webRequest.url}\n{requestOp.webRequest.downloadHandler.text}");
				callback(response, null);
			};
		}
	}
}
