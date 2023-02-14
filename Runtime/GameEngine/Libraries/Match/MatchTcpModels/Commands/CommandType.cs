namespace MatchTcpModels.Commands
{
	public enum CommandType
	{
		None = 0,
		AuthenticateMatchUserSecret,
		JoinMatch,
		InGameData,
		PingServerResponse,
		PingClient,
		Unknown,
		AuthenticateAsSpectator,
		AuthenticateUnreliableSessionToken
	}
}
