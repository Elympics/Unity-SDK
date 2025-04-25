using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Matchmaking;
namespace Elympics.ElympicsSystems.Internal
{
    internal class ReconnectingState : ElympicsLobbyClientState
    {
        private readonly ElympicsLoggerContext _logger;

        //TODO take this value from config with min value of 1.
        private const int ReconnectAttempts = 1;
        private bool _isReconnecting;
        public ReconnectingState(ElympicsLobbyClient client, ElympicsLoggerContext logger) : base(client)
        {
            State = ElympicsState.Reconnecting;
            _logger = logger.WithContext(nameof(ReconnectingState));
        }
        public override UniTask Connect(ConnectionData data)
        {
            ElympicsLogger.LogWarning(GenerateWarningMessage(nameof(Connect)));
            return UniTask.CompletedTask;
        }
        public override async UniTask ReConnect(ConnectionData reconnectionData)
        {
            if (_isReconnecting)
                return;

            _isReconnecting = true;
            var logger = _logger.WithMethodName();
            var isSuccess = false;
            for (var counter = 0; counter < ReconnectAttempts; counter++)
            {
                {
                    try
                    {
                        logger.Log($"Try to reconnect. Attempt: #{counter + 1}");
                        Client.CheckConnectionDataOrThrow(reconnectionData);
                        await Client.Authorize(reconnectionData);
                        await Client.ConnectToLobby(reconnectionData);
                        await Client.RoomsManager.CheckJoinedRoomStatus();
                        isSuccess = true;
                        break;

                    }
                    catch (Exception e)
                    {
                        logger.Warning($"Failed to reconnect on attempt #{counter + 1}: {e.Message}");
                        ++counter;
                    }
                    finally
                    {
                        _isReconnecting = false;
                    }
                }
            }

            if (isSuccess)
                OnSuccess();
            else
                OnFailure();


            void OnSuccess()
            {
                if (Client.RoomsManager.ListJoinedRooms().Count > 0)
                {
                    if (Client.RoomsManager.ListJoinedRooms()[0].IsDuringMatchmaking())
                        Client.SwitchState(ElympicsState.Matchmaking);
                    else if (Client.GameplaySceneMonitor.IsCurrentlyInMatch)
                        Client.SwitchState(ElympicsState.PlayingMatch);
                    else
                        Client.SwitchState(ElympicsState.Connected);
                }
                else
                    Client.SwitchState(ElympicsState.Connected);

                Client.OnSuccessfullyConnectedToElympics(true);
            }

            void OnFailure()
            {
                logger.Error("Failed to reconnect to Elympics.");
                Client.SignOutInternal();
                Client.SwitchState(ElympicsState.Disconnected);
            }
        }
        public override UniTask SignOut() => throw new ElympicsException(GenerateErrorMessage(nameof(SignOut)));
        public override UniTask StartMatchmaking(IRoom room) => throw new ElympicsException(GenerateErrorMessage(nameof(StartMatchmaking)));
        public override UniTask CancelMatchmaking(IRoom room, CancellationToken ct = default)
        {
            ElympicsLogger.LogWarning(GenerateWarningMessage(nameof(CancelMatchmaking)));
            return UniTask.CompletedTask;
        }
        public override UniTask PlayMatch(MatchmakingFinishedData matchData) => throw new ElympicsException(GenerateErrorMessage(nameof(PlayMatch)));
        public override UniTask FinishMatch() => UniTask.CompletedTask;
        public override void MatchFound() => throw new ElympicsException(GenerateErrorMessage(nameof(MatchFound)));
    }
}
