using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Rooms.PublicModels;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Lobby.Models;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics
{
    internal class RoomsClient : IRoomsClient
    {
        private readonly ElympicsLoggerContext _logger;
        public event Action<GameDataResponse>? GameDataResponse;
        public event Action<RoomStateChanged>? RoomStateChanged;
        public event Action<LeftRoomArgs>? LeftRoom;
        public event Action<RoomListChanged>? RoomListChanged;

        public RoomsClient(ElympicsLoggerContext logger) => _logger = logger.WithContext(nameof(RoomsClient));

        public SessionConnectionDetails SessionConnectionDetails =>
            Session?.ConnectionDetails ?? throw new InvalidOperationException("Missing WebSocket session object.");

        public IWebSocketSessionInternal? Session
        {
            private get => _session;
            set
            {
                if (_session != null)
                    _session.MessageReceived -= HandleMessage;
                _session = value;
                if (_session != null)
                    _session.MessageReceived += HandleMessage;
            }
        }

        private IWebSocketSessionInternal? _session;

        private void HandleMessage(IFromLobby message)
        {
            switch (message)
            {
                case GameDataResponse response:
                    GameDataResponse?.Invoke(response);
                    return;
                case RoomStateChanged stateChanged:
                    RoomStateChanged?.Invoke(stateChanged);
                    return;
                case RoomWasLeft roomLeft:
                    LeftRoom?.Invoke(new LeftRoomArgs(roomLeft.RoomId, roomLeft.Reason));
                    return;
                case RoomListChanged roomListChanged:
                    RoomListChanged?.Invoke(roomListChanged);
                    return;
                default:
                    return;
            }
        }

        public UniTask<Guid> CreateRoom(
            string roomName,
            bool isPrivate,
            bool isEphemeral,
            string queueName,
            bool isSingleTeam,
            IReadOnlyDictionary<string, string> customRoomData,
            IReadOnlyDictionary<string, string> customMatchmakingData,
            RoomBetDetailsParam? betDetails = null,
            CancellationToken ct = default)
        {
            var betSlim = GetRoomBetDetailsSlim(betDetails);

            _logger.WithMethodName().Log($"Create room {roomName}");
            return ExecuteOperation<RoomIdOperationResult>(new CreateRoom(roomName, isPrivate, isEphemeral, queueName, isSingleTeam, customRoomData, customMatchmakingData, null, betSlim),
                    ct)
                .ContinueWith(result => result.RoomId);
        }

        public UniTask<Guid> JoinRoom(Guid roomId, uint? teamIndex, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log($"Join room {roomId}");
            return ExecuteOperation<RoomIdOperationResult>(new JoinWithRoomId(roomId, teamIndex), ct)
                .ContinueWith(result => result.RoomId);
        }

        public UniTask<Guid> JoinRoom(string joinCode, uint? teamIndex, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log("Join room using join code.");
            return ExecuteOperation<RoomIdOperationResult>(new JoinWithJoinCode(joinCode, teamIndex), ct)
                .ContinueWith(result => result.RoomId);
        }

        public UniTask ChangeTeam(Guid roomId, uint? teamIndex, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log($"Set new team {teamIndex}.");
            return ExecuteOperation(new ChangeTeam(roomId, teamIndex), ct);
        }

        public UniTask SetReady(Guid roomId, byte[] gameEngineData, float[] matchmakerData, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log("Set ready.");
            return ExecuteOperation(new SetReady(roomId, gameEngineData, matchmakerData), ct);
        }

        public UniTask SetUnready(Guid roomId, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log("Set unready.");
            return ExecuteOperation(new SetUnready(roomId), ct);
        }

        public UniTask LeaveRoom(Guid roomId, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log("Leave room.");
            return ExecuteOperation(new LeaveRoom(roomId), ct);
        }

        public UniTask UpdateRoomParams(
            Guid roomId,
            Guid hostId,
            string? roomName,
            bool? isPrivate,
            IReadOnlyDictionary<string, string>? customRoomData,
            IReadOnlyDictionary<string, string>? customMatchmakingData,
            RoomBetDetailsParam? betDetails = null,
            CancellationToken ct = default)
        {
            var betSlim = GetRoomBetDetailsSlim(betDetails);
            return ExecuteOperationHostOnly(hostId, new SetRoomParameters(roomId, roomName, isPrivate, customRoomData, customMatchmakingData, null, betSlim), ct);
        }

        public UniTask StartMatchmaking(Guid roomId, Guid hostId)
        {
            _logger.WithMethodName().Log("Start matchmaking.");
            return ExecuteOperationHostOnly(hostId, new StartMatchmaking(roomId), default);
        }

        public UniTask CancelMatchmaking(Guid roomId, CancellationToken ct = default)
        {
            _logger.WithMethodName().Log("Cancel matchmaking.");
            return ExecuteOperation(new CancelMatchmaking(roomId), ct);
        }

        private enum RoomWatchingState
        {
            NotWatching = 0,
            Watching,
            WatchRequestSent,
            UnwatchRequestSent,
        }

        private RoomWatchingState _roomWatchingState = RoomWatchingState.NotWatching;

        public async UniTask WatchRooms(CancellationToken ct = default)
        {
            if (_roomWatchingState != RoomWatchingState.NotWatching)
                throw new InvalidOperationException($"Cannot request watching rooms in {_roomWatchingState} state");
            _roomWatchingState = RoomWatchingState.WatchRequestSent;
            try
            {
                await ExecuteOperation(new WatchRooms(), ct);
                _roomWatchingState = RoomWatchingState.Watching;
            }
            catch
            {
                _roomWatchingState = RoomWatchingState.NotWatching;
                throw;
            }
        }

        public async UniTask UnwatchRooms(CancellationToken ct = default)
        {
            if (_roomWatchingState != RoomWatchingState.Watching)
                throw new InvalidOperationException($"Cannot request watching rooms in {_roomWatchingState} state");
            _roomWatchingState = RoomWatchingState.UnwatchRequestSent;
            try
            {
                await ExecuteOperation(new UnwatchRooms(), ct);
                _roomWatchingState = RoomWatchingState.NotWatching;
            }
            catch
            {
                _roomWatchingState = RoomWatchingState.Watching;
                throw;
            }
        }

        private UniTask ExecuteOperation(LobbyOperation message, CancellationToken ct) =>
            ExecuteOperation<OperationResult>(message, ct);

        private async UniTask<T> ExecuteOperation<T>(LobbyOperation message, CancellationToken ct)
            where T : OperationResult
        {
            if (Session == null)
                throw new InvalidOperationException("Missing WebSocket session object.");
            ct.ThrowIfCancellationRequested();
            var result = await Session.ExecuteOperation(message, ct);
            if (result is T expectedResult)
                return expectedResult;
            throw new UnexpectedRoomResultException(typeof(T), result.GetType());
        }

        private UniTask ExecuteOperationHostOnly(Guid? roomHostId, LobbyOperation message, CancellationToken ct, [CallerMemberName] string methodName = "")
        {
            if (Session == null)
                throw new InvalidOperationException("Missing WebSocket session object.");
            if (roomHostId == null)
                throw new RoomPrivilegeException($"Cannot call ${methodName} on rooms without host.");
            var expectedUserId = SessionConnectionDetails.AuthData!.UserId;
            if (expectedUserId != null && roomHostId != expectedUserId)
                throw new RoomPrivilegeException($"Only hosts can call ${methodName} method.");

            return Session.ExecuteOperation(message, ct);
        }

        private static RoomBetDetailsSlim? GetRoomBetDetailsSlim(RoomBetDetailsParam? betDetails)
        {
            RoomBetDetailsSlim? betSlim = null;
            if (betDetails.HasValue)
            {
                var coinDecimal = ElympicsLobbyClient.Instance!.FetchDecimalForCoin(betDetails.Value.CoinId);
                if (coinDecimal == null)
                    throw new ArgumentException($"Couldn't create bet with CoinId: {betDetails.Value.CoinId}");

                betSlim = new RoomBetDetailsSlim(WeiConverter.ToWei(betDetails.Value.BetValue, coinDecimal.Value), betDetails.Value.CoinId);
            }
            return betSlim;
        }
    }
}
