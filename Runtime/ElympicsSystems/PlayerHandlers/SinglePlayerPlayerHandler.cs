using System.Collections.Generic;
using System.Linq;
using MatchTcpClients.Synchronizer;

namespace Elympics.ElympicsSystems
{
    internal class SinglePlayerPlayerHandler : IServerPlayerHandler
    {
        private readonly ElympicsServer _server;
        private readonly GameEngineAdapter _gameEngineAdapter;
        private readonly ElympicsBehavioursManager _elympicsBehavioursManager;
        private ElympicsPlayer[] _players;
        private ElympicsPlayer[] _bots;

        public SinglePlayerPlayerHandler(ElympicsServer server, GameEngineAdapter gameEngineAdapter, ElympicsBehavioursManager elympicsBehavioursManager)
        {
            _server = server;
            _gameEngineAdapter = gameEngineAdapter;
            _elympicsBehavioursManager = elympicsBehavioursManager;
        }

        public void InitializePlayersOnServer(InitialMatchPlayerDatasGuid initialData)
        {
            var dataOfBots = initialData.Where(x => x.IsBot).ToList();
            _elympicsBehavioursManager.OnBotsOnServerInit(new InitialMatchPlayerDatasGuid(dataOfBots));

            _bots = dataOfBots.Select(x => x.Player).ToArray();
            CallPlayerConnectedFromBotsOrClients(_bots);

            var dataOfClients = initialData.Where(x => !x.IsBot).ToList();
            _elympicsBehavioursManager.OnClientsOnServerInit(new InitialMatchPlayerDatasGuid(dataOfClients));

            _players = dataOfClients.Select(x => x.Player).ToArray();
            CallPlayerConnectedFromBotsOrClients(_players);
            _elympicsBehavioursManager.OnConnected(TimeSynchronizationData.Localhost);
            _elympicsBehavioursManager.OnAuthenticated(dataOfClients[0].UserId);
            _elympicsBehavioursManager.OnMatchJoined(initialData.MatchId.GetValueOrDefault());
        }


        private void CallPlayerConnectedFromBotsOrClients(IEnumerable<ElympicsPlayer> players)
        {
            foreach (var player in players)
                _elympicsBehavioursManager.OnPlayerConnected(player);
        }
        public void RetrieveInput(long tick)
        {
            using (ElympicsMarkers.Elympics_GatheringClientInputMarker.Auto())
            {
                if (_bots != null)
                    foreach (var bot in _bots)
                    {
                        _server.SwitchBehaviourToBot(bot);
                        var input = _elympicsBehavioursManager.OnInputForBot();
                        input.Tick = tick;
                        input.Player = bot;
                        _gameEngineAdapter.AddBotsOrClientsInServerInputToBuffer(input);
                    }

                // ReSharper disable once InvertIf
                if (_players != null)
                    foreach (var player in _players)
                    {
                        _server.SwitchBehaviourToClient(player);
                        var input = _elympicsBehavioursManager.OnInputForClient();
                        input.Tick = tick;
                        input.Player = player;
                        _gameEngineAdapter.AddBotsOrClientsInServerInputToBuffer(input);
                    }

                _server.SwitchBehaviourToServer();
            }
        }
    }
}
