using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Matchmaking;
namespace Elympics.ElympicsSystems.Internal
{
    internal class ConnectedState : ElympicsLobbyClientState
    {
        public ConnectedState(ElympicsLobbyClient client) : base(client)
        {
            State = ElympicsState.Connected;
        }
        public override async UniTask Connect(ConnectionData data)
        {
            Client.SwitchState(ElympicsState.Connecting);
            await Client.CurrentState.Connect(data);
        }
        public override async UniTask SignOut()
        {
            Client.SignOutInternal();
            Client.SwitchState(ElympicsState.Disconnected);
            await UniTask.CompletedTask;
        }
        public override async UniTask StartMatchmaking(IRoom room)
        {
            try
            {
                await room.StartMatchmakingInternal();
                Client.SwitchState(ElympicsState.Matchmaking);
            }
            catch (Exception)
            {
                Client.SwitchState(ElympicsState.Connected);
                throw;
            }
        }
        public override async UniTask PlayMatch(MatchmakingFinishedData matchData)
        {
            Client.SwitchState(ElympicsState.PlayingMatch);
            await Client.CurrentState.PlayMatch(matchData);
        }
        public override async UniTask WatchReplay()
        {
            Client.SwitchState(ElympicsState.WatchReplay);
            await Client.CurrentState.WatchReplay();
        }

        public override async UniTask ReConnect(ConnectionData reconnectionData)
        {
            Client.SwitchState(ElympicsState.Reconnecting);
            await Client.CurrentState.ReConnect(reconnectionData);
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
