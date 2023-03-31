namespace Elympics.Models.Matchmaking
{
	public static class Routes
	{
		public const string Base = "matchmaking";

		public const string GetMatchLongPolling        = "getMatchLongPolling";
		public const string GetPendingMatchLongPolling = "getPendingMatchLongPolling";
		public const string GetUnfinishedMatches       = "getUnfinishedMatches";

		public const string FindAndJoinMatch           = "findAndJoinMatch";
	}
}
