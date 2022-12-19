using System;
using Daftmobile.Api;

namespace LobbyPublicApiModels.User.Matchmaking
{
	public static class JoinMatchmakerAndWaitForMatchModel
	{
		public static class ErrorCodes
		{
			public const string NonExistentUser                = "NonExistentUser";
			public const string JoiningQueueFailed             = "Joining matchmaker queue failed";
			public const string InitialUserDataExternalBackend = "InitialUserDataExternalBackend: ";
			public const string StillWaiting                   = "Still waiting for match";
			public const string OpponentNotFound               = "Opponent not found";
			public const string MatchmakerQueueNotFound        = "Requested matchmaker queue not found";
			public const string GameNotFound                   = "Game with provided id not found";
			public const string GameVersionConfigNotFound      = "Requested game version config not found";
			public const string GameVersionBlocked             = "Requested game version is blocked";
			public const string DeserializationFailed          = "Deserialization of MatchmakerQueue/GameVersionConfig failed";
		}

		[Serializable]
		public class Request : ApiRequest
		{
			public string  GameId;
			public string  GameVersion;
			public byte[]  GameEngineData;
			public float[] MatchmakerData;
			public string  QueueName;
			public string  RegionName;

			public override bool IsValid => !string.IsNullOrEmpty(GameId) && !string.IsNullOrEmpty(GameVersion);
		}

		[Serializable]
		public class Response : ApiResponse
		{
			public string MatchId;
		}
	}
}
