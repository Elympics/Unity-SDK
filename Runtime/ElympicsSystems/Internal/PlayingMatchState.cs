using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Matchmaking;

namespace Elympics.ElympicsSystems.Internal
{
    internal class PlayingMatchState : ElympicsLobbyClientState
    {
        public PlayingMatchState(ElympicsLobbyClient client) : base(client)
        {
            State = ElympicsState.PlayingMatch;
        }
        public override UniTask Connect(ConnectionData data) => throw new ElympicsException(GenerateErrorMessage(nameof(Connect)));
        public override UniTask SignOut() => throw new ElympicsException(GenerateErrorMessage(nameof(SignOut)));
        public override UniTask Disconnect()
        {
            //Client.ClearAuthData();
            Client.SwitchState(ElympicsState.Disconnected);
            return UniTask.CompletedTask;
        }
        public override UniTask StartMatchmaking(IRoom room) => throw new ElympicsException(GenerateErrorMessage(nameof(StartMatchmaking)));
        public override UniTask PlayMatch(MatchmakingFinishedData matchData)
        {
            if (Client.GameplaySceneMonitor.IsCurrentlyInMatch)
                throw new InvalidOperationException("Game is already on the gameplay scene.");

            Client.PlayMatchInternal(matchData ?? throw new ArgumentNullException(nameof(matchData)));
            return UniTask.CompletedTask;
        }
        public override UniTask WatchReplay() => throw new ElympicsException(GenerateErrorMessage(nameof(WatchReplay)));
        public override async UniTask Reconnect(ConnectionData reconnectionData)
        {
            Client.SwitchState(ElympicsState.Reconnecting);
            await Client.CurrentState.Reconnect(reconnectionData);
        }
        public override async UniTask FinishMatch()
        {
            Client.FinishMatchInternal();
            Client.SwitchState(ElympicsState.Connected);
            await UniTask.CompletedTask;
        }
        public override void MatchFound() => ElympicsLogger.LogWarning(GenerateWarningMessage(nameof(MatchFound)));
        public override async UniTask CancelMatchmaking(IRoom room, CancellationToken ct = default)
        {
            ElympicsLogger.LogWarning(GenerateWarningMessage(nameof(CancelMatchmaking)));
            await UniTask.CompletedTask;
        }
    }
}
