using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Matchmaking;

namespace Elympics.ElympicsSystems.Internal
{
    internal class WatchReplayState : ElympicsLobbyClientState
    {
        public WatchReplayState(ElympicsLobbyClient client) : base(client)
        {
        }
        public override UniTask Connect(ConnectionData data) => throw new ElympicsException(GenerateErrorMessage(nameof(Connect)));
        public override UniTask ReConnect(ConnectionData reconnectionData) => throw new ElympicsException(GenerateErrorMessage(nameof(ReConnect)));
        public override UniTask SignOut() => throw new ElympicsException(GenerateErrorMessage(nameof(SignOut)));
        public override UniTask StartMatchmaking(IRoom room) => throw new ElympicsException(GenerateErrorMessage(nameof(StartMatchmaking)));
        public override UniTask CancelMatchmaking(IRoom room, CancellationToken ct = default)
        {
            ElympicsLogger.LogWarning(GenerateWarningMessage(nameof(CancelMatchmaking)));
            return UniTask.CompletedTask;
        }
        public override UniTask PlayMatch(MatchmakingFinishedData matchData) => throw new ElympicsException(GenerateErrorMessage(nameof(PlayMatch)));
        public override UniTask WatchReplay()
        {
            Client.WatchReplayInternal();
            return UniTask.CompletedTask;
        }
        public override UniTask FinishMatch()
        {
            Client.SwitchState(ElympicsState.Connected);
            return UniTask.CompletedTask;
        }
        public override void MatchFound()
        {
        }
    }
}
