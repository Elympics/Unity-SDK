using System;
using System.Threading;
using System.Threading.Tasks;
using LobbyPublicApiModels.User.Auth;
using LobbyPublicApiModels.User.ClientProfiling;
using LobbyPublicApiModels.User.Matchmaking;

namespace LobbyPublicApiClients.Abstraction.User
{
	public interface IUserApiClient
	{
		void SetServerUri(string host, int port);
		void SetServerUri(string address);
		void SetAuthToken(string authToken);

		Task<AuthenticateUserIdModel.Response> AuthenticateUserIdAsync(AuthenticateUserIdModel.Request request, CancellationToken ct = default);
		void                                   AuthenticateUserIdAsync(AuthenticateUserIdModel.Request request, Action<AuthenticateUserIdModel.Response, Exception> callback, CancellationToken ct = default);

		Task<SetAttemptReferenceModel.Response> SetAttemptReferenceAsync(SetAttemptReferenceModel.Request request, CancellationToken ct = default);
		void                                    SetAttemptReferenceAsync(SetAttemptReferenceModel.Request request, Action<SetAttemptReferenceModel.Response, Exception> callback, CancellationToken ct = default);

		Task<JoinMatchmakerAndWaitForMatchModel.Response> JoinMatchmakerAndWaitForMatchAsync(JoinMatchmakerAndWaitForMatchModel.Request request, CancellationToken ct = default);
		void JoinMatchmakerAndWaitForMatchAsync(JoinMatchmakerAndWaitForMatchModel.Request request, Action<JoinMatchmakerAndWaitForMatchModel.Response, Exception> callback, CancellationToken ct = default);

		Task<GetMatchModel.Response> GetMatchLongPollingAsync(GetMatchModel.Request request, CancellationToken ct = default);
		void                         GetMatchLongPollingAsync(GetMatchModel.Request request, Action<GetMatchModel.Response, Exception> callback, CancellationToken ct = default);

		Task<GetUnfinishedMatchesModel.Response> GetUnfinishedMatchesAsync(GetUnfinishedMatchesModel.Request request, CancellationToken ct = default);
		void                                     GetUnfinishedMatchesAsync(GetUnfinishedMatchesModel.Request request, Action<GetUnfinishedMatchesModel.Response, Exception> callback, CancellationToken ct = default);
	}
}
