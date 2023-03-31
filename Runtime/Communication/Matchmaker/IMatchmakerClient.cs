using System;
using System.Threading;
using Elympics.Models.Matchmaking;

namespace Elympics
{
	internal abstract class MatchmakerClient : IMatchmakerEvents
	{
		protected readonly IUserApiClient UserApiClient;

		internal MatchmakerClient(IUserApiClient userApiClient) => UserApiClient = userApiClient;

		internal abstract void JoinMatchmakerAsync(JoinMatchmakerData joinMatchmakerData, CancellationToken ct = default);

		#region Matchmaking events

		internal void EmitMatchmakingStarted() =>
			AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingStarted?.Invoke());
		internal void EmitMatchmakingFinished(MatchmakingFinishedData data) =>
			AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingFinished?.Invoke(data));
		internal void EmitMatchmakingMatchFound(Guid matchId) =>
			AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingMatchFound?.Invoke(matchId));
		internal void EmitMatchmakingError(string error, Guid matchId) =>
			AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingError?.Invoke((error, matchId)));
		internal void EmitMatchmakingWarning(string warning, Guid matchId) =>
			AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingWarning?.Invoke((warning, matchId)));
		internal void EmitMatchmakingCancelled(Guid matchId) =>
			AsyncEventsDispatcher.Instance.Enqueue(() => MatchmakingCancelled?.Invoke(matchId));

		public event Action MatchmakingStarted;
		public event Action<MatchmakingFinishedData> MatchmakingFinished;
		public event Action<Guid> MatchmakingMatchFound;
		public event Action<(string Error, Guid MatchId)> MatchmakingError;
		public event Action<(string Warning, Guid MatchId)> MatchmakingWarning;
		public event Action<Guid> MatchmakingCancelled;

		#endregion
	}
}
