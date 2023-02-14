namespace MatchTcpModels.Messages
{
	public enum MessageType
	{
		None = 0,
		Connected,
		PingServer,
		InGameData,
		MatchJoined,
		UserMatchAuthenticatedMessage,
		PingClientResponse,
		UnknownCommandMessage,
		MatchEnded,
		AuthenticateAsSpectator
	}
}
