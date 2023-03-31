using System;
using System.Threading;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking.LongPolling;
using HybridWebSocket;

namespace Elympics
{
	internal interface IUserApiClient
	{
		string ServerUri { set; }
		string ClientSecret { set; }
		string JwtToken { get; set; }
		string UserId { get; set; }

		void AuthenticateWithClientSecret(AuthenticateUserIdModel.Request request, Action<AuthenticateUserIdModel.Response, Exception> callback, CancellationToken ct = default);
		void JoinMatchmakerAndWaitForMatch(JoinMatchmakerAndWaitForMatchModel.Request request, Action<JoinMatchmakerAndWaitForMatchModel.Response, Exception> callback, CancellationToken ct = default);
		void GetMatchLongPolling(GetMatchModel.Request request, Action<GetMatchModel.Response, Exception> callback, CancellationToken ct = default);
		IWebSocket CreateMatchmakingWebSocket();
	}
}
