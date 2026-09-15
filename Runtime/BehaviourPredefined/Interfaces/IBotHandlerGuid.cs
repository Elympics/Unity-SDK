namespace Elympics
{
    public interface IBotHandlerGuid : IObservable
    {
        /// <summary>
        /// Called on bots-in-server initialization (after processing all initial <see cref="ElympicsBehaviour"/>s).
        /// </summary>
        /// <param name="initialMatchPlayerDatas">Initialization data of all bots included in the server.</param>
        /// <remarks>Used in "Local Player And Bots" development mode or if "Bots inside server" option is checked.</remarks>
        void OnBotsOnServerInit(InitialMatchPlayerDatasGuid initialMatchPlayerDatas);
    }
}
