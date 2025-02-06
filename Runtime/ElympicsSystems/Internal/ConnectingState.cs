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
                await Client.FetchAvailableRegions();
                await Client.Authorize(data);
                await Client.ConnectToLobby(data);
                await Client.RoomsManager.CheckJoinedRoomStatus();
                Client.SwitchState(ElympicsState.Connected);
            }
            catch
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
        public override UniTask StartMatchmaking(IRoom room) => throw new ElympicsException(GenerateErrorMessage(nameof(StartMatchmaking)));
        public override UniTask PlayMatch(MatchmakingFinishedData matchData) => throw new ElympicsException(GenerateErrorMessage(nameof(PlayMatch)));
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
