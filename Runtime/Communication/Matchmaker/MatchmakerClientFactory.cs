namespace Elympics
{
    internal static class MatchmakerClientFactory
    {
        internal static MatchmakerClient Create(ElympicsGameConfig gameConfig, string baseUrl) =>
            gameConfig.UseLegacyMatchmaking
                ? new LongPollingMatchmakerClient(baseUrl)
                : new WebSocketMatchmakerClient(baseUrl);
    }
}
