using System;
using System.Collections.Generic;
using Daftmobile.Api;

namespace LobbyPublicApiModels.User.Matchmaking
{
	public static class GetUnfinishedMatchesModel
	{
		public static class ErrorCodes
		{
		}

		[Serializable]
		public class Request : ApiRequest
		{
			public override bool IsValid => true;
		}

		[Serializable]
		public class Response : ApiResponse
		{
			public List<string> MatchesIds;
		}
	}
}
