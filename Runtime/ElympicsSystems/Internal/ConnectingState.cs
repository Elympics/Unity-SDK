using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Matchmaking;

namespace Elympics.ElympicsSystems.Internal
{
    internal class ConnectingState : ElympicsLobbyClientState
    {
        private bool _performingConnection;
        public ConnectingState(ElympicsLobbyClient client) : base(client)
        {
            State = ElympicsState.Connecting;
        }
        public override async UniTask Connect(ConnectionData data)
        {
            if (_performingConnection)
                throw new ElympicsException("Already connecting to Elympics.");
            _performingConnection = true;
            try
            {
                Client.CheckConnectionDataOrThrow(data);
                await Client.Authorize(data);
                await Client.FetchAvailableRegions();
                await Client.ConnectToLobby(data);
                await Client.RoomsManager.CheckJoinedRoomStatus();
                await Client.GetElympicsUserData();
                if (Client.RoomsManager.CurrentRoom?.State.MatchmakingData?.MatchmakingState is Rooms.Models.MatchmakingState.Matchmaking or Rooms.Models.MatchmakingState.RequestingMatchmaking)
                    Client.SwitchState(ElympicsState.Matchmaking);
                else if (Client.GameplaySceneMonitor.IsCurrentlyInMatch)
                    Client.SwitchState(ElympicsState.PlayingMatch);
                else
                    Client.SwitchState(ElympicsState.Connected);
                Client.OnSuccessfullyConnectedToElympics(false);
            }
            catch (Exception)
            {
                Client.SwitchState(ElympicsState.Disconnected);
                throw;
            }
            finally
            {
                _performingConnection = false;
            }
        }
        public override UniTask SignOut() => throw new ElympicsException(GenerateErrorMessage(nameof(SignOut)));
        public override UniTask Disconnect() => UniTask.CompletedTask;
        public override UniTask StartMatchmaking(IRoom room) => throw new ElympicsException(GenerateErrorMessage(nameof(StartMatchmaking)));
        public override UniTask PlayMatch(MatchmakingFinishedData matchData) => throw new ElympicsException(GenerateErrorMessage(nameof(PlayMatch)));
        public override UniTask WatchReplay() => throw new ElympicsException(GenerateErrorMessage(nameof(WatchReplay)));
        public override UniTask ReConnect(ConnectionData connection) => throw new ElympicsException(GenerateErrorMessage(nameof(ReConnect)));
        public override async UniTask FinishMatch()
        {
            ElympicsLogger.LogWarning(GenerateWarningMessage(nameof(FinishMatch)));
            await UniTask.CompletedTask;
        }
        public override void MatchFound()
        { }
        public override async UniTask CancelMatchmaking(IRoom room, CancellationToken ct = default)
        {
            ElympicsLogger.LogWarning(GenerateWarningMessage(nameof(CancelMatchmaking)));
            await UniTask.CompletedTask;
        }
    }
}
