using Elympics.Models.Authentication;

namespace Elympics
{
	internal static class MatchmakerClientFactory
	{
		internal static MatchmakerClient Create(ElympicsGameConfig gameConfig, string baseUrl) =>
			gameConfig.UseLegacyMatchmaking
				? (MatchmakerClient)new LongPollingMatchmakerClient(baseUrl)
				: new WebSocketMatchmakerClient(baseUrl);
	}
}
