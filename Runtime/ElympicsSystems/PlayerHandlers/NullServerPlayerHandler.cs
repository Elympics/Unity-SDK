#nullable enable

namespace Elympics.ElympicsSystems
{
    internal class NullServerPlayerHandler : IServerPlayerHandler
    {
        public void InitializePlayersOnServer(InitialMatchPlayerDatasGuid initialMatchPlayerDatasGuid) { }
        public void RetrieveInput(long tick) { }
    }
}
