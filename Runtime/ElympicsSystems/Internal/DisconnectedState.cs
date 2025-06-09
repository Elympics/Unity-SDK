using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Matchmaking;
using UnityEngine;

namespace Elympics.ElympicsSystems.Internal
{
    internal class DisconnectedState : ElympicsLobbyClientState
    {
        public DisconnectedState(ElympicsLobbyClient client) : base(client)
        {
            State = ElympicsState.Disconnected;
        }
        public override async UniTask Connect(ConnectionData data)
        {
            Client.SwitchState(ElympicsState.Connecting);
            await Client.CurrentState.Connect(data);
        }
        public override async UniTask SignOut()
        {
            Debug.LogWarning(GenerateWarningMessage(nameof(SignOut)));
            await UniTask.CompletedTask;
        }
        public override UniTask Disconnect() => UniTask.CompletedTask;
        public override async UniTask StartMatchmaking(IRoom room)
        {
            ElympicsLogger.LogError(GenerateErrorMessage(nameof(StartMatchmaking)));
            await UniTask.CompletedTask;
        }
        public override async UniTask PlayMatch(MatchmakingFinishedData matchData)
        {
            ElympicsLogger.LogError(GenerateErrorMessage(nameof(StartMatchmaking)));
            await UniTask.CompletedTask;

        }
        public override UniTask ReConnect(ConnectionData reconnectionData) => UniTask.CompletedTask;
        public override async UniTask WatchReplay()
        {
            ElympicsLogger.LogError(GenerateErrorMessage(nameof(WatchReplay)));
            await UniTask.CompletedTask;
        }
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
