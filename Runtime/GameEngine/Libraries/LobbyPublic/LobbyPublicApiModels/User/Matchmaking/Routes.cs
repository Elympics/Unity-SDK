using System;

namespace LobbyPublicApiModels.User.Matchmaking
{
	public static class Routes
	{
		public const string Base = "matchmaking";

		[Obsolete] public const string AcceptMatch                      = "acceptMatch";
		[Obsolete] public const string GetPendingMatchResultLongPolling = "getPendingMatchResultLongPolling";

		public const string GetMatchLongPolling        = "getMatchLongPolling";
		public const string GetPendingMatchLongPolling = "getPendingMatchLongPolling";
		public const string GetUnfinishedMatches       = "getUnfinishedMatches";
	}
}
