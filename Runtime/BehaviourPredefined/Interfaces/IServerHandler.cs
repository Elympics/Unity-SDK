namespace Elympics
{
	public interface IServerHandler : IObservable
	{
		/// <summary>
		/// Called on server initialization (after processing all initial <see cref="ElympicsBehaviour"/>s).
		/// </summary>
		/// <param name="initialMatchPlayerDatas">Initialization data of all possible clients and bots.</param>
		void OnServerInit(InitialMatchPlayerDatas initialMatchPlayerDatas);

		/// <summary>
		/// Called when a client/bot disconnects from the server.
		/// </summary>
		/// <param name="player">Identifier of disconnected player.</param>
		void OnPlayerDisconnected(ElympicsPlayer player);

		/// <summary>
		/// Called when a client/bot connects to the server.
		/// </summary>
		/// <param name="player">Identifier of connected player.</param>
		void OnPlayerConnected(ElympicsPlayer player);
	}
}
