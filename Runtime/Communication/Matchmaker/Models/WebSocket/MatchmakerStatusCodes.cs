namespace Elympics.Models.Matchmaking.WebSocket
{
	public enum MatchmakerStatusCodes
	{
		NormalClosure = 1000,
		EndpointUnavailable = 1001,
		InvalidMessageType = 1003,
		InternalServerError = 1011,

		Unauthorized = 4000,

		GameDoesNotExist = 4100,
		GameVersionDoesNotExist = 4101,
		QueueDoesNotExist = 4102,
		RegionDoesNotExist = 4103,

		ExternalGameBackendJoinError = 4200,
		ExternalGameBackendJoinRefused = 4201,

		OpponentNotFound = 4300,
		MatchmakingCanceled = 4301,

		MatchCreationFailed = 4400,
		MatchPreparationFailed = 4401,

		NoMatchToRejoin = 4500,
	}
}
