#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Matchmaking;

namespace Elympics.ElympicsSystems.Internal
{
    internal class MatchmakingState : ElympicsLobbyClientState
    {
        private bool _matchmakingCancelRequested;
        public MatchmakingState(ElympicsLobbyClient client) : base(client) => State = ElympicsState.Matchmaking;
        public override UniTask Connect(ConnectionData data) => throw new ElympicsException(GenerateErrorMessage(nameof(Connect)));
        public override UniTask SignOut() => throw new ElympicsException(GenerateErrorMessage(nameof(SignOut)));

        public override UniTask Disconnect()
        {
            //Client.ClearAuthData();
            Client.SwitchState(ElympicsState.Disconnected);
            return UniTask.CompletedTask;
        }
        public override UniTask StartMatchmaking(IRoom room) => throw new ElympicsException(GenerateErrorMessage(nameof(StartMatchmaking)));
        public override UniTask PlayMatch(MatchmakingFinishedData matchData) => throw new ElympicsException(GenerateErrorMessage(nameof(PlayMatch)));
        public override UniTask WatchReplay() => throw new ElympicsException(GenerateErrorMessage(nameof(WatchReplay)));
        public override async UniTask ReConnect(ConnectionData reconnectionData)
        {
            _matchmakingCancelRequested = false;
            Client.SwitchState(ElympicsState.Reconnecting);
            await Client.CurrentState.ReConnect(reconnectionData);
        }

        public override async UniTask FinishMatch()
        {
            ElympicsLogger.LogWarning(GenerateWarningMessage(nameof(FinishMatch)));
            await UniTask.CompletedTask;
        }
        public override void MatchFound() => Client.SwitchState(ElympicsState.Connected);
        public override async UniTask CancelMatchmaking(IRoom room, CancellationToken ct = default)
        {
            if (_matchmakingCancelRequested)
                throw new ElympicsException("Matchmaking cancellation already requested");
            try
            {
                _matchmakingCancelRequested = true;
                await room.CancelMatchmakingInternal(ct);
                Client.SwitchState(ElympicsState.Connected);
                _matchmakingCancelRequested = false;

            }
            catch (LobbyOperationException e)
            {
                if (e.Kind != ErrorKind.RoomAlreadyInMatchedState)
                {
                    Client.SwitchState(ElympicsState.Connected);
                    throw;
                }
                _matchmakingCancelRequested = false;
            }
        }
    }
}
