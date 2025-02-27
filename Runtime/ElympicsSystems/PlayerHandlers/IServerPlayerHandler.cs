namespace Elympics.ElympicsSystems
{
    internal interface IServerPlayerHandler
    {
        void InitializePlayersOnServer(InitialMatchPlayerDatasGuid initialMatchPlayerData);
        void RetrieveInput(long tick);
    }
}
