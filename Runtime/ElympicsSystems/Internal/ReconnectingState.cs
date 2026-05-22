using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Core.Logger;
using Elympics.Models.Matchmaking;

namespace Elympics.ElympicsSystems.Internal
{
    internal class ReconnectingState : ElympicsLobbyClientState
    {
        private readonly ElympicsLoggerConfig _logger;

        //TODO take this value from config with min value of 1.
        private const int ReconnectAttempts = 1;
        private bool _isReconnecting;

        public ReconnectingState(ElympicsLobbyClient client, ElympicsLoggerConfig logger) : base(client)
        {
            State = ElympicsState.Reconnecting;
            _logger = logger.WithClassName(nameof(ReconnectingState));
        }

        public override UniTask Connect(ConnectionData data)
        {
            ElympicsLogger.LogWarning(GenerateWarningMessage(nameof(Connect)));
            return UniTask.CompletedTask;
        }

        public override async UniTask Reconnect(ConnectionData reconnectionData)
        {
            if (_isReconnecting)
                return;

            _isReconnecting = true;
            var logger = _logger.WithMehodName();
            var isSuccess = false;
            for (var counter = 0; counter < ReconnectAttempts; counter++)
            {
                {
                    try
                    {
                        logger.LogInfo($"Try to reconnect. Attempt: #{counter + 1}");
                        Client.CheckConnectionDataOrThrow(reconnectionData);
                        await Client.Authorize(reconnectionData);
                        var gameData = await Client.ConnectToLobby(reconnectionData);
                        if (gameData is not null)
                            await Client.InitializeBasedOnGameData(gameData);
                        await Client.GetElympicsUserData();
                        isSuccess = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        logger.LogWarning($"Failed to reconnect on attempt #{counter + 1}: {e.Message}");
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
                if (Client.RoomsManager.CurrentRoom != null)
                {
                    if (Client.RoomsManager.CurrentRoom.IsDuringMatchmaking())
                        Client.SwitchState(ElympicsState.Matchmaking);
                    else if (Client.GameplaySceneMonitor!.IsCurrentlyInMatch)
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
                logger.LogError("Failed to reconnect to Elympics.");
                Client.SignOutInternal();
                Client.SwitchState(ElympicsState.Disconnected);
            }
        }

        public override UniTask SignOut() => throw new ElympicsException(GenerateErrorMessage(nameof(SignOut)));
        public override UniTask Disconnect() => UniTask.CompletedTask;
        public override UniTask StartMatchmaking(IRoom room) => throw new ElympicsException(GenerateErrorMessage(nameof(StartMatchmaking)));

        public override UniTask CancelMatchmaking(IRoom room, CancellationToken ct = default)
        {
            ElympicsLogger.LogWarning(GenerateWarningMessage(nameof(CancelMatchmaking)));
            return UniTask.CompletedTask;
        }

        public override UniTask PlayMatch(MatchmakingFinishedData matchData) => throw new ElympicsException(GenerateErrorMessage(nameof(PlayMatch)));
        public override UniTask WatchReplay() => throw new ElympicsException(GenerateErrorMessage(nameof(WatchReplay)));
        public override UniTask FinishMatch() => UniTask.CompletedTask;
        public override void MatchFound() => throw new ElympicsException(GenerateErrorMessage(nameof(MatchFound)));
    }
}
