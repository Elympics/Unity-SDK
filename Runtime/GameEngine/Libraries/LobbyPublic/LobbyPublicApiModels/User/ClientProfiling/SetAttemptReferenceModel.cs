using System;
using Daftmobile.Api;

namespace LobbyPublicApiModels.User.ClientProfiling
{
	public static class SetAttemptReferenceModel
	{
		public static class ErrorCodes
		{
			public const string PlayerNotInMatchError = "PlayerNotInMatch";
		}

		[Serializable]
		public class Request : ApiRequest
		{
			public string AttemptReference;

			public override bool IsValid => AttemptReference != null;
		}

		[Serializable]
		public class Response : ApiResponse
		{
		}
	}
}
