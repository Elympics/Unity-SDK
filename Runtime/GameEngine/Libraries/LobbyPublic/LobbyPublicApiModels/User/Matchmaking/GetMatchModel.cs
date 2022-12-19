using System;
using System.Collections.Generic;
using Daftmobile.Api;

namespace LobbyPublicApiModels.User.Matchmaking
{
	public static class GetMatchModel
	{
		public static class ErrorCodes
		{
			public const string MatchDoesntExist        = "Match doesn't exist";
			public const string PlayerNotInMatchError   = "Player not in match";
			public const string UserSecretNotFoundError = "User secret not found";
			public const string NotInDesiredState       = "Not in desired state";
			public const string MatchError              = "Match error";
		}

		[Serializable]
		public class Request : ApiRequest
		{
			public string                MatchId;
			public GetMatchDesiredState? DesiredState;

			public override bool IsValid => MatchId != null && DesiredState != null;
		}

		[Serializable]
		public class Response : ApiResponse
		{
			public string       MatchId;
			public string       ServerAddress;
			public string       TcpUdpServerAddress;
			public string       WebServerAddress;
			public string       UserSecret;
			public List<string> MatchedPlayersId;
		}
	}
}
