using System;

namespace LobbyPublicApiModels.User.Matchmaking
{
	public enum GetMatchDesiredState
	{
		Initializing,

		// Backwards compatibility - to remove ~pprzestrzelski 23.07.2020
		[Obsolete] Starting,
		Running,
		Ended
	}
}