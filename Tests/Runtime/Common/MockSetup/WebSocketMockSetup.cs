#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Lobby.Models.FromLobby;
using Elympics.Communication.Lobby.Models.ToLobby;
using Elympics.Lobby.Models;
using Elympics.Rooms.Models;
using HybridWebSocket;
using MessagePack;
using NSubstitute;

namespace Elympics
{
    internal static class WebSocketMockSetup
    {
        public static readonly WebSocketMockBackendSession WebSocketMockBackendSession;

        private static readonly IAuthClient AuthClient;
        private static CancellationTokenSource? cts;
        private static CancellationTokenSource? pingCts;
        private static bool isInRoom;
        private static IWebSocket ws;

        // source https://github.com/Thundernerd/Unity3D-NSubstitute/blob/main/Editor/NSubstitute.dll
        static WebSocketMockSetup() => WebSocketMockBackendSession = new WebSocketMockBackendSession();

        private class MessageHandledException : Exception
        { }

        public static IWebSocket SetupToLobbyOperations(
            this IWebSocket ws,
            Guid userId,
            string nickname,
            string? avatarUrl)
        {
            ws.When(x => x.Send(Arg.Any<byte[]>())).Do(x =>
            {
                var data = (byte[])x[0];
                var msg = MessagePackSerializer.Deserialize<IToLobby>(data);
                ElympicsLogger.Log($"[MOCK] Received message type {msg.GetType().Name}");
                try
                {
                    switch (msg)
                    {
                        case CreateRoom createRoom:
                        {
                            ThrowIfAlreadyInRoom(ws, createRoom);
                            var (teamSize, teamCount) = GetQueueOrThrow(createRoom.QueueName, ws, createRoom);

                            var room = new RoomStateChanged(Guid.NewGuid(),
                                DateTime.Now,
                                createRoom.RoomName,
                                "ZZZZZZZZ",
                                true,
                                new MatchmakingData(DateTime.Now, MatchmakingState.Unlocked, createRoom.QueueName, teamCount, teamSize, createRoom.CustomMatchmakingData, null, null, null),
                                new List<UserInfo>
                                {
                                    new(userId, 0, false, nickname, avatarUrl),
                                },
                                createRoom.IsPrivate,
                                createRoom.IsEphemeral,
                                createRoom.CustomRoomData);

                            WebSocketMockBackendSession.PlayerCurrentRoom = room.RoomId;
                            WebSocketMockBackendSession.Rooms[room.RoomId] = room;

                            SendSuccessResponse(ws, createRoom, room.RoomId);
                            UpdateTime(ref room);
                            SendResponse(ws, room);

                            break;
                        }
                        case JoinWithRoomId joinWithRoomId:
                        {
                            ThrowIfAlreadyInRoom(ws, joinWithRoomId);
                            var room = GetRoomOrThrow(joinWithRoomId.RoomId, ws, joinWithRoomId);
                            ThrowIfTeamFull(joinWithRoomId.TeamIndex, room, ws, joinWithRoomId);

                            WebSocketMockBackendSession.PlayerCurrentRoom = room.RoomId;
                            UpdateUsers(ref room, room.Users.Append(new UserInfo(userId, joinWithRoomId.TeamIndex, false, nickname, avatarUrl)));
                            SendSuccessResponse(ws, joinWithRoomId, room.RoomId);
                            UpdateTime(ref room);
                            SendResponse(ws, room);
                            break;
                        }
                        case JoinWithJoinCode joinWithJoinCode:
                        {
                            ThrowIfAlreadyInRoom(ws, joinWithJoinCode);
                            var room = GetRoomOrThrow(joinWithJoinCode.JoinCode, ws, joinWithJoinCode);
                            WebSocketMockBackendSession.PlayerCurrentRoom = room.RoomId;
                            UpdateUsers(ref room, room.Users.Append(new UserInfo(userId, joinWithJoinCode.TeamIndex, false, nickname, avatarUrl)));
                            SendSuccessResponse(ws, joinWithJoinCode, room.RoomId);
                            UpdateTime(ref room);
                            SendResponse(ws, room);
                            break;
                        }
                        case LeaveRoom leaveRoom:
                        {
                            ThrowIfNotInRoom(ws, leaveRoom);
                            var room = GetRoomOrThrow(leaveRoom.RoomId, ws, leaveRoom);

                            WebSocketMockBackendSession.PlayerCurrentRoom = null;
                            UpdateUsers(ref room, room.Users.Where(u => u.UserId != userId));

                            SendSuccessResponse(ws, leaveRoom);
                            UpdateTime(ref room);
                            SendResponse(ws, new RoomWasLeft(room.RoomId, LeavingReason.UserLeft));
                            UpdateRoomOnList(ws, room);
                            break;
                        }
                        case SetRoomParameters setRoomParameters:
                        {
                            ThrowIfNotInRoom(ws, setRoomParameters);
                            var room = GetCurrentRoom();

                            room = room with
                            {
                                RoomName = setRoomParameters.RoomName ?? room.RoomName,
                                IsPrivate = setRoomParameters.IsPrivate ?? room.IsPrivate,
                                CustomData = setRoomParameters.CustomRoomData ?? room.CustomData,
                                MatchmakingData = room.MatchmakingData! with
                                {
                                    CustomData = setRoomParameters.CustomMatchmakingData ?? room.MatchmakingData.CustomData,
                                },
                            };

                            SendSuccessResponse(ws, setRoomParameters);
                            UpdateTime(ref room);
                            SendResponse(ws, room);
                            UpdateRoomOnList(ws, room);
                            break;
                        }
                        case ChangeTeam changeTeam:
                        {
                            ThrowIfNotInRoom(ws, changeTeam);
                            var room = GetCurrentRoom();
                            ThrowIfTeamFull(changeTeam.TeamIndex, room, ws, changeTeam);

                            UpdateUsers(ref room, room.Users.Select(u => u.UserId == userId ? u with { TeamIndex = changeTeam.TeamIndex } : u));
                            SendSuccessResponse(ws, changeTeam);
                            UpdateTime(ref room);
                            SendResponse(ws, room);
                            break;
                        }
                        case SetReady setReady:
                        {
                            ThrowIfNotInRoom(ws, setReady);
                            var room = GetCurrentRoom();
                            var user = room.Users.Single(u => u.UserId == userId);
                            ThrowIfAlreadyReady(user, ws, setReady);
                            UpdateUsers(ref room,
                                room.Users.Select(u => u.UserId == userId ? u with
                                {
                                    IsReady = true
                                } : u));
                            SendSuccessResponse(ws, setReady);
                            UpdateTime(ref room);
                            SendResponse(ws, room);
                            break;
                        }
                        case SetUnready setUnready:
                        {
                            ThrowIfNotInRoom(ws, setUnready);
                            var room = GetCurrentRoom();
                            var user = room.Users.Single(u => u.UserId == userId);
                            ThrowIfAlreadyUnReady(user, ws, setUnready);
                            UpdateUsers(ref room,
                                room.Users.Select(u => u.UserId == userId ? u with
                                {
                                    IsReady = false
                                } : u));
                            SendSuccessResponse(ws, setUnready);
                            UpdateTime(ref room);
                            SendResponse(ws, room);
                            break;
                        }
                        case StartMatchmaking startMatchmaking:
                        {
                            ThrowIfAlreadyInMatchmaking(ws, startMatchmaking);
                            cts = new CancellationTokenSource();
                            ThrowIfNotInRoom(ws, startMatchmaking);
                            var room = GetCurrentRoom();
                            ThrowIfNoMatchmakingInRoom(ws, room, startMatchmaking);
                            ThrowIfNotHost(ws, userId, room, startMatchmaking);
                            ThrowIfAllNotReady(ws, room, startMatchmaking);
                            room = room with
                            {
                                MatchmakingData = room.MatchmakingData! with
                                {
                                    State = MatchmakingState.RequestingMatchmaking,
                                },
                            };
                            WebSocketMockBackendSession.Rooms[room.RoomId] = room;
                            SendSuccessResponse(ws, startMatchmaking);
                            UpdateTime(ref room);
                            SendResponse(ws, room);
                            try
                            {
                                SimulateMatchmaking(ws, room, cts.Token);
                            }
                            catch (OperationCanceledException)
                            {
                                ElympicsLogger.Log($"[MOCK] Canceled matchmaking.");
                            }

                            break;
                        }
                        case CancelMatchmaking cancelMatchmaking:
                        {
                            ThrowIfNotInRoom(ws, cancelMatchmaking);
                            var room = GetCurrentRoom();
                            ThrowIfNoMatchmakingStarted(ws, cancelMatchmaking);
                            ThrowIfNoMatchmakingInRoom(ws, room, cancelMatchmaking);
                            ThrowIfCannotCancel(ws, room, cancelMatchmaking);
                            ThrowIfNotHost(ws, userId, room, cancelMatchmaking);
                            cts?.Cancel();
                            cts = null;
                            room = room with
                            {
                                MatchmakingData = room.MatchmakingData! with
                                {
                                    State = MatchmakingState.CancellingMatchmaking,
                                },
                            };
                            WebSocketMockBackendSession.Rooms[room.RoomId] = room;
                            SendSuccessResponse(ws, cancelMatchmaking);
                            UpdateTime(ref room);
                            SendResponse(ws, room);
                            SimulateCancelling(ws, room);
                            break;
                        }
                        case WatchRooms watchRooms:
                        {
                            ThrowIfMatchingWatchListState(ws, watchRooms);
                            WebSocketMockBackendSession.TracksRoomList = true;
                            SendSuccessResponse(ws, watchRooms);
                            SendResponse(ws,
                                new RoomListChanged(new List<ListedRoomChange>(WebSocketMockBackendSession.Rooms.Select(r => new ListedRoomChange(r.Key, CreatePublicRoomState(r.Value))))));
                            break;
                        }
                        case UnwatchRooms unwatchRooms:
                        {
                            ThrowIfMatchingWatchListState(ws, unwatchRooms);
                            WebSocketMockBackendSession.TracksRoomList = false;
                            SendSuccessResponse(ws, unwatchRooms);
                            break;
                        }
                        case ShowAuth showAuth:
                        {
                            SendSuccessResponse(ws, showAuth);
                            SendResponseInternal(ws, new ShowAuthResponse(Guid.Empty, "some-auth-type", "some-eth-address", "some-nickname", "some-avatar-url"));
                            break;
                        }
                        case Ping ping:
                            break;
                        case JoinLobby joinLobby:
                            break;
                        case Pong pong:
                        {
                            break;
                        }
                        default:
                            throw new NotImplementedException($"[MOCK] No handler for message of type {msg.GetType().FullName}");
                    }
                }
                catch (MessageHandledException)
                {
                    // finish
                }
            });
            WebSocketMockSetup.ws = ws;
            return ws;
        }


        public static IWebSocket SetPingDelayMessage(this IWebSocket ws, double delay)
        {
            pingCts = new CancellationTokenSource();
            SchedulePingMessage(ws, delay, pingCts.Token).Forget();
            return ws;
        }

        public static IWebSocket SetupJoinLobby(
            this IWebSocket ws,
            bool createInitialRoom,
            Guid userId,
            string nickname,
            string? avatarUrl)
        {
            ws.When(x => x.Send(Arg.Any<byte[]>())).Do(x =>
            {
                var data = (byte[])x[0];
                var msg = MessagePackSerializer.Deserialize<IToLobby>(data);
                ElympicsLogger.Log($"[MOCK] Received message type {msg.GetType().Name}");
                try
                {
                    switch (msg)
                    {
                        case JoinLobby joinLobby:
                        {
                            SendSuccessResponse(ws, joinLobby);
                            var gameResponse = new GameDataResponse(createInitialRoom ? 1 : 0, new List<RoomCoin>(), string.Empty, string.Empty);
                            SendResponse(ws, gameResponse);
                            if (createInitialRoom)
                            {
                                var room = new RoomStateChanged(Guid.NewGuid(),
                                    DateTime.Now,
                                    "RoomName",
                                    "ZZZZZZZZ",
                                    true,
                                    new MatchmakingData(DateTime.Now, MatchmakingState.Unlocked, "QueueName", 1, 2, new Dictionary<string, string>(), null, null, null),
                                    new List<UserInfo>
                                    {
                                        new(userId, 0, false, nickname, avatarUrl),
                                    },
                                    true,
                                    false,
                                    new Dictionary<string, string>());

                                WebSocketMockBackendSession.PlayerCurrentRoom = room.RoomId;
                                WebSocketMockBackendSession.Rooms[room.RoomId] = room;
                                SendResponse(ws, room);
                            }
                            break;
                        }
                        case ShowAuth showAuth:
                            break;
                        case CancelMatchmaking cancelMatchmaking:
                            break;
                        case ChangeTeam changeTeam:
                            break;
                        case CreateRoom createRoom:
                            break;
                        case JoinWithJoinCode joinWithJoinCode:
                            break;
                        case JoinWithRoomId joinWithRoomId:
                            break;
                        case LeaveRoom leaveRoom:
                            break;
                        case SetReady setReady:
                            break;
                        case SetRoomParameters setRoomParameters:
                            break;
                        case SetUnready setUnready:
                            break;
                        case StartMatchmaking startMatchmaking:
                            break;
                        case UnwatchRooms unwatchRooms:
                            break;
                        case WatchRooms watchRooms:
                            break;
                        case LobbyOperation lobbyOperation:
                            break;
                        case Ping ping:
                            break;
                        case Pong pong:
                            break;
                        default:
                            throw new NotImplementedException($"[MOCK] No handler for message of type {msg.GetType().FullName}");
                    }
                }
                catch (MessageHandledException)
                {
                    // finish
                }
            });
            WebSocketMockSetup.ws = ws;
            return ws;
        }

        public static IWebSocket SetupOpenCloseDefaultBehaviour(this IWebSocket ws)
        {
            ws.When(x => x.Connect()).Do(async _ =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.5), DelayType.Realtime);
                ElympicsLogger.Log("[MOCK] Connect called");
                ws.OnOpen += Raise.Event<WebSocketOpenEventHandler>();
            });

            ws.When(x => x.Close(Arg.Any<WebSocketCloseCode>(), Arg.Any<string>())).Do(info =>
            {
                var reason = (WebSocketCloseCode)info.Args()[0];
                var details = (string)info.Args()[1];
                ElympicsLogger.Log("[MOCK] Closed called");
                ws.OnClose += Raise.Event<WebSocketCloseEventHandler>(reason, details);
                pingCts?.Cancel();
            });
            return ws;
        }
        private static async UniTask SchedulePingMessage(IWebSocket ws, double delay, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                SendResponse(ws, new Ping());
                await UniTask.Delay(TimeSpan.FromSeconds(delay), DelayType.Realtime);
            }
        }

        public static void MakeAllPlayersReadyForRoom(Guid roomId, Guid userId)
        {
            var room = WebSocketMockBackendSession.Rooms[roomId];
            UpdateUsers(ref room,
                room.Users.Select(u => u.UserId != userId ? u with
                {
                    IsReady = true
                } : u));
            UpdateTime(ref room);
            SendResponse(ws, room);
        }

        public static void SimulateRoomParametersChange(Guid roomId, string? roomName, bool? isPrivate, Dictionary<string, string>? customRoomData, Dictionary<string, string>? customMatchmakingData)
        {
            var room = WebSocketMockBackendSession.Rooms[roomId];
            room = room with
            {
                RoomName = roomName ?? room.RoomName,
                IsPrivate = isPrivate ?? room.IsPrivate,
                CustomData = customRoomData ?? room.CustomData,
                MatchmakingData = room.MatchmakingData! with
                {
                    CustomData = customMatchmakingData ?? room.MatchmakingData.CustomData,
                },
            };

            UpdateTime(ref room);
            if (WebSocketMockBackendSession.PlayerCurrentRoom == roomId)
                SendResponse(ws, room);
            UpdateRoomOnList(ws, room);
        }

        public static void CancelPingToken() => pingCts?.Cancel();

        private static void UpdateUsers(ref RoomStateChanged roomStateChanged, IEnumerable<UserInfo> users)
        {
            roomStateChanged = roomStateChanged with
            {
                Users = users.ToList(),
            };
            WebSocketMockBackendSession.Rooms[roomStateChanged.RoomId] = roomStateChanged;
        }

        private static void UpdateTime(ref RoomStateChanged roomStateChanged)
        {
            roomStateChanged = roomStateChanged with
            {
                LastUpdate = DateTime.UtcNow,
            };
            WebSocketMockBackendSession.Rooms[roomStateChanged.RoomId] = roomStateChanged;
        }

        private static async void SimulateCancelling(IWebSocket ws, RoomStateChanged room)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.Realtime);
            SetMatchmakingState(ref room, MatchmakingState.Unlocked);
            UpdateTime(ref room);
            SendResponse(ws, room);
        }

        private static async void SimulateMatchmaking(IWebSocket ws, RoomStateChanged room, CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.Realtime, PlayerLoopTiming.Update, ct);
                if (ct.IsCancellationRequested)
                    return;
                SetMatchmakingState(ref room, MatchmakingState.RequestingMatchmaking);
                UpdateTime(ref room);
                SendResponse(ws, room);

                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.Realtime, PlayerLoopTiming.Update, ct);
                if (ct.IsCancellationRequested)
                    return;
                SetMatchmakingState(ref room, MatchmakingState.Matchmaking);
                UpdateTime(ref room);
                SendResponse(ws, room);

                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.Realtime, PlayerLoopTiming.Update, ct);
                if (ct.IsCancellationRequested)
                    return;
                SetMatchmakingState(ref room, MatchmakingState.Playing);
                room = room with
                {
                    MatchmakingData = room.MatchmakingData! with
                    {
                        MatchData = GetDummyMatchData(room.Users.Select(x => x.UserId).ToList()),
                    },
                };
                UpdateTime(ref room);
                SendResponse(ws, room);

                await UniTask.Delay(TimeSpan.FromSeconds(5), DelayType.Realtime, PlayerLoopTiming.Update, ct);
                if (ct.IsCancellationRequested)
                    return;
                SetMatchmakingState(ref room, MatchmakingState.Unlocked);
                UpdateTime(ref room);
                SendResponse(ws, room);
                cts = null;
            }
            catch (OperationCanceledException)
            {
                ElympicsLogger.Log("[MOCK] Cancelling matchmaking simulation.");
            }
        }

        private static void SetMatchmakingState(ref RoomStateChanged room, MatchmakingState newState)
        {
            ElympicsLogger.Log($"[MOCK] Matchmaking state set to <color=green>{newState}</color>");
            room = room with
            {
                MatchmakingData = room.MatchmakingData! with
                {
                    State = newState,
                },
            };
            WebSocketMockBackendSession.Rooms[room.RoomId] = room;
        }


        private static void ThrowIfAlreadyInMatchmaking(IWebSocket ws, StartMatchmaking startMatchmaking, Guid? roomId = null)
        {
            if (cts == null)
                return;
            SendFailResponse(ws, startMatchmaking, ErrorBlame.UserError, ErrorKind.Unspecified, roomId, $"Matchmaking is already in progress.");
            throw new MessageHandledException();
        }

        private static void ThrowIfNoMatchmakingStarted(IWebSocket ws, LobbyOperation matchmakingOperation, Guid? roomId = null)
        {
            if (cts != null)
                return;
            SendFailResponse(ws, matchmakingOperation, ErrorBlame.UserError, ErrorKind.Unspecified, roomId, $"Cannot cancel when no matchmaking is in progress.");
            throw new MessageHandledException();
        }

        private static void ThrowIfCannotCancel(IWebSocket ws, RoomStateChanged room, LobbyOperation matchmakingOperation, Guid? roomId = null)
        {
            if (room.MatchmakingData!.State is MatchmakingState.Matchmaking or MatchmakingState.CancellingMatchmaking)
                return;
            SendFailResponse(ws,
                matchmakingOperation,
                ErrorBlame.UserError,
                ErrorKind.Unspecified,
                roomId,
                $"Can't cancel matchmaking in states different than {MatchmakingState.Matchmaking} or {MatchmakingState.CancellingMatchmaking}.");
            throw new MessageHandledException();
        }

        private static void ThrowIfNoMatchmakingInRoom(IWebSocket ws, RoomStateChanged room, LobbyOperation matchmakingOperation, Guid? roomId = null)
        {
            if (room.MatchmakingData == null)
                SendFailResponse(ws, matchmakingOperation, ErrorBlame.UserError, ErrorKind.RoomWithoutMatchmaking, roomId, "Cant's start matchmaking in non-matchmaking room.");
            throw new MessageHandledException();
        }

        private static void ThrowIfNotHost(IWebSocket ws, Guid hostId, RoomStateChanged room, LobbyOperation matchmakingOperation, Guid? roomId = null)
        {
            if (room.Users[0].UserId == hostId)
                return;
            SendFailResponse(ws, matchmakingOperation, ErrorBlame.UserError, ErrorKind.NotHost, roomId, "Only host can start matchmaking");
            throw new MessageHandledException();
        }

        private static void ThrowIfAllNotReady(IWebSocket ws, RoomStateChanged room, StartMatchmaking startMatchmaking, Guid? roomId = null)
        {
            if (room.Users.Any(x => !x.IsReady))
                SendFailResponse(ws, startMatchmaking, ErrorBlame.UserError, ErrorKind.NotEveryoneReady, roomId, "Not all users are ready");
            throw new MessageHandledException();
        }

        private static void ThrowIfAlreadyUnReady(UserInfo user, IWebSocket ws, SetUnready setUnready, Guid? roomId = null)
        {
            if (user.IsReady)
                return;
            SendFailResponse(ws, setUnready, ErrorBlame.UserError, ErrorKind.AlreadyNotReady, roomId, "User is already unready.");
            throw new MessageHandledException();
        }

        private static void ThrowIfAlreadyReady(UserInfo user, IWebSocket ws, SetReady setReady, Guid? roomId = null)
        {
            if (!user.IsReady)
                return;
            SendFailResponse(ws, setReady, ErrorBlame.UserError, ErrorKind.AlreadyReady, roomId, "User is already ready.");
            throw new MessageHandledException();
        }

        private static RoomStateChanged GetCurrentRoom() => WebSocketMockBackendSession.Rooms[WebSocketMockBackendSession.PlayerCurrentRoom!.Value];

        private static (uint TeamSize, uint TeamCount) GetQueueOrThrow(string queueName, IWebSocket ws, LobbyOperation createRoom, Guid? roomId = null)
        {
            if (WebSocketMockBackendSession.Queues.TryGetValue(queueName, out var queue))
                return queue;
            SendFailResponse(ws, createRoom, ErrorBlame.UserError, ErrorKind.QueueDoesNotExist, roomId);
            throw new MessageHandledException();
        }

        private static void ThrowIfTeamFull(uint? teamIndex, RoomStateChanged room, IWebSocket ws, LobbyOperation joinWithRoomId)
        {
            if (teamIndex == null
                || room.MatchmakingData == null
                || room.Users.Count(u => u.TeamIndex == teamIndex) < room.MatchmakingData.TeamCount)
                return;
            SendFailResponse(ws, joinWithRoomId, ErrorBlame.UserError, ErrorKind.TeamFull, room.RoomId, "Team full");
            throw new MessageHandledException();
        }

        private static RoomStateChanged GetRoomOrThrow(Guid roomId, IWebSocket ws, LobbyOperation joinWithRoomId)
        {
            if (WebSocketMockBackendSession.Rooms.TryGetValue(roomId, out var room))
                return room;
            SendFailResponse(ws, joinWithRoomId, ErrorBlame.UserError, ErrorKind.RoomDoesNotExist, roomId, $"Room {roomId} doesn't exists");
            throw new MessageHandledException();
        }

        private static RoomStateChanged GetRoomOrThrow(string joinCode, IWebSocket ws, LobbyOperation joinWithRoomId)
        {
            foreach (var (_, state) in WebSocketMockBackendSession.Rooms)
                if (state.JoinCode == joinCode)
                    return state;

            SendFailResponse(ws, joinWithRoomId, ErrorBlame.UserError, ErrorKind.RoomDoesNotExist, null, $"Room with given JoinCode {joinCode} doesn't exists");
            throw new MessageHandledException();
        }

        private static void ThrowIfAlreadyInRoom(IWebSocket ws, LobbyOperation createRoom)
        {
            if (WebSocketMockBackendSession.PlayerCurrentRoom == null)
                return;
            SendFailResponse(ws, createRoom, ErrorBlame.UserError, ErrorKind.AlreadyInRoom, WebSocketMockBackendSession.PlayerCurrentRoom, "Already in room");
            throw new MessageHandledException();
        }

        private static void ThrowIfNotInRoom(IWebSocket ws, LobbyOperation leaveRoom)
        {
            if (WebSocketMockBackendSession.PlayerCurrentRoom != null)
                return;
            SendFailResponse(ws, leaveRoom, ErrorBlame.UserError, ErrorKind.NotInRoom, null, "Not in room");
            throw new MessageHandledException();
        }

        private static void ThrowIfMatchingWatchListState(IWebSocket ws, LobbyOperation trackingState)
        {
            if ((trackingState is WatchRooms && !WebSocketMockBackendSession.TracksRoomList)
                || (trackingState is UnwatchRooms && WebSocketMockBackendSession.TracksRoomList))
                return;
            SendFailResponse(ws, trackingState, ErrorBlame.UserError, ErrorKind.Unspecified, null, $"Room list tracking already in state {trackingState}");
            throw new MessageHandledException();
        }

        private static void SendSuccessResponse(IWebSocket ws, LobbyOperation lobbyOperation, Guid? roomId = null)
        {
            ElympicsLogger.Log($"[MOCK] Sending response success on {lobbyOperation.GetType().Name} OperationId: {lobbyOperation.OperationId}");
            if (lobbyOperation is CreateRoom or JoinWithJoinCode or JoinWithRoomId)
                SendResponseInternal(ws, new RoomIdOperationResult(lobbyOperation.OperationId, roomId!.Value));
            else
                SendResponseInternal(ws, new OperationResult(lobbyOperation.OperationId));
        }

        private static void SendFailResponse(
            IWebSocket ws,
            LobbyOperation lobbyOperation,
            ErrorBlame blame,
            ErrorKind kind,
            Guid? roomId = null,
            string? details = null)
        {
            ElympicsLogger.Log($"[MOCK] Sending response fail on {lobbyOperation.GetType().Name} with {blame}, {kind}, {details}");
            if (lobbyOperation is CreateRoom or JoinWithJoinCode or JoinWithRoomId)
                SendResponseInternal(ws, new RoomIdOperationResult(lobbyOperation.OperationId, blame, kind, details, roomId!.Value));
            else
                SendResponseInternal(ws, new OperationResult(lobbyOperation.OperationId, blame, kind, details));
        }

        private static void SendResponse(IWebSocket ws, IFromLobby fromLobby)
        {
            ElympicsLogger.Log($"[MOCK] Sending {fromLobby.GetType().Name}");
            SendResponseInternal(ws, fromLobby);
        }

        private static void SendResponseInternal(IWebSocket ws, IFromLobby fromLobby) => ws.OnMessage += Raise.Event<WebSocketMessageEventHandler>(MessagePackSerializer.Serialize(fromLobby));

        private static void UpdateRoomOnList(IWebSocket ws, RoomStateChanged room) => SendResponse(ws,
            new RoomListChanged(new List<ListedRoomChange>
            {
                new(room.RoomId, CreatePublicRoomState(room))
            }));

        private static MatchData GetDummyMatchData(IReadOnlyList<Guid> matchedPlayers) =>
            new(Guid.NewGuid(), MatchState.Initializing, GetDummyMatchDetails(matchedPlayers), null);

        private static MatchDetails GetDummyMatchDetails(IReadOnlyList<Guid> matchedPlayers) =>
            new(matchedPlayers, string.Empty, string.Empty, string.Empty, Array.Empty<byte>(), Array.Empty<float>());

        private static PublicRoomState CreatePublicRoomState(RoomStateChanged roomState)
        {
            var matchmakingData = new PublicMatchmakingData(roomState.MatchmakingData!.LastStateUpdate,
                roomState.MatchmakingData!.State,
                roomState.MatchmakingData.QueueName,
                roomState.MatchmakingData.TeamCount,
                roomState.MatchmakingData.TeamSize,
                roomState.MatchmakingData.CustomData,
                roomState.MatchmakingData.BetDetails);

            return new PublicRoomState(roomState.RoomId,
                roomState.LastUpdate,
                roomState.RoomName,
                roomState.HasPrivilegedHost,
                matchmakingData,
                roomState.Users,
                roomState.IsPrivate,
                roomState.CustomData);
        }
    }
}
