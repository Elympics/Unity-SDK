namespace Elympics
{
	internal static class MatchmakerClientFactory
	{
		internal static MatchmakerClient Create(ElympicsGameConfig gameConfig, IUserApiClient apiClient) =>
			gameConfig.UseLegacyMatchmaking
				? (MatchmakerClient)new LongPollingMatchmakerClient(apiClient)
				: new WebSocketMatchmakerClient(apiClient);
	}
}
