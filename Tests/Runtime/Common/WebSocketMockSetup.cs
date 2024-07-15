using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Lobby.Models;
using Elympics.Models.Authentication;
using Elympics.Rooms.Models;
using HybridWebSocket;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;

#nullable enable
namespace Elympics
{
    internal static class WebSocketMockSetup
    {
        private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private const string Nickname = "nickname";

        private const string FakeJwt = @"{
  ""header"": {
    ""alg"": ""RS256"",
    ""typ"": ""JWT""
  },
  ""payload"": {
    ""nameid"": ""057f2883-b4b4-4cc6-895f-e1332da86567"",
    ""auth-type"": ""client-secret"",
    ""nbf"": 1718803982,
    ""exp"": 111,
    ""iat"": 1718803982
  },
  ""signature"": ""rX85CHYGCpo2V1J6hXRj0rRySi-n7qxjiuwS98P9zS6W-hfKHKsApWJQeLUZ4_0DCUr8AE-YdkbYESKwv6Jl5OuyHDH4QCIVuTkCVrbT4duCiopitcVqwNubQARpTc7lApDAxihAtmdVUuUwz26po2ntlgv-p_JdHqN1g5Uk3vr9miKDdBzvSwSWwN1NP2cGEvzqlAs3wHtw4GYZChX_RugjM-vppuovQMOkwxJ7IvQXV7kb00ucpj71u9EmTmQFN9RMnB8b4c5K7-kXCM-_L2PNAC6MZX2-OExNWklQtqTUD3oF-dJFRH4Hew_ZEgt_SBw37NWN1NSfT2q1wnXh0TDpFPPnZSqYUGNYl7mhOlLrPWNi5e4dpiawy-23760qDmj4kriyqOPcVCzWTbmcvcEe-ktwBIo9MNwYZvQCFJ7yZfsdVTlw7WdBO9_Kf6JZNVZ7Rc6jjCN3OPmCJShTLg7GbiHOp9Bl8637mXXV7GwTzqZxoyAvU9ysRyRXC3kMkUEew0oyAr8eCXU1k-8DIiK_AYdzAUIqSfgV74MwONqQtmrxbGx8kw_l4D15ha7vOMI0QoN9Tu62ElFBgwk2j-1ysH7_7D_sx-9wYD-gUUaOIgL2e71cLzxzzQ0RJYh984BE6RawW4-mzjiR3J8g9NYPRhT-911w-F_HGRTXCZ4""
}";

        public static readonly WebSocketMockBackendSession WebSocketMockBackendSession;

        private static readonly IAuthClient AuthClient;
        private static CancellationTokenSource? cts;
        private static readonly IWebSocket Ws = Substitute.For<IWebSocket>();
        private static bool isInRoom;

        // source https://github.com/Thundernerd/Unity3D-NSubstitute/blob/main/Editor/NSubstitute.dll
        static WebSocketMockSetup()
        {
            WebSocketMockBackendSession = new WebSocketMockBackendSession();
            AuthClient = CreateAuthClientMock();
            WebSocket = CreateMockWebSocket();
        }

#pragma warning disable IDE0060
        internal static IWebSocket WebSocket;
#pragma warning restore IDE0060
        internal static IAuthClient MockAuthClient() => AuthClient;

        private static IAuthClient CreateAuthClientMock()
        {
            var token = EncodeJwtFromJson(FakeJwt);
            var taskResult = UniTask.FromResult(Result<AuthData, string>.Success(new AuthData(UserId, token, Nickname, AuthType.ClientSecret)));
            var ac = Substitute.For<IAuthClient>();
            _ = ac.AuthenticateWithClientSecret(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(taskResult);
            return ac;
        }

        private class MessageHandledException : Exception
        { }

        public static string EncodeJwtFromJson(string json)
        {
            var jwtObject = JObject.Parse(json);

            var expireTime = DateTime.UtcNow.AddHours(1);
            var epochTimeSpan = expireTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var epochTime = (long)epochTimeSpan.TotalSeconds;
            var header = jwtObject["header"]!.ToString(Formatting.None);
            jwtObject!["payload"]!["exp"] = epochTime;
            var payload = jwtObject["payload"]!.ToString(Formatting.None);
            var signature = jwtObject["signature"]!.ToString();

            var encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(header));
            var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
            var encodedSignature = Base64UrlEncode(Encoding.UTF8.GetBytes(signature));

            return $"{encodedHeader}.{encodedPayload}.{encodedSignature}";
        }

        public static string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input);
            output = output.Replace('+', '-'); // Replace '+' with '-'
            output = output.Replace('/', '_'); // Replace '/' with '_'
            output = output.TrimEnd('='); // Remove any trailing '='
            return output;
        }

        private static IWebSocket CreateMockWebSocket()
        {

            Ws.When(x => x.Connect()).Do(_ =>
            {
                ElympicsLogger.Log("[MOCK] Connect called");
                Ws.OnOpen += Raise.Event<WebSocketOpenEventHandler>();
            });

            Ws.When(x => x.Send(Arg.Any<byte[]>())).Do(x =>
            {
                var data = (byte[])x[0];
                var msg = MessagePackSerializer.Deserialize<IToLobby>(data);
                ElympicsLogger.Log($"[MOCK] Received message type {msg.GetType().Name}");
                try
                {
                    switch (msg)
                    {
                        case Pong pong:
                        {
                            break;
                        }
                        case JoinLobby joinLobby:
                        {
                            SendSuccessResponse(Ws, joinLobby);
                            var gameResponse = new GameDataResponse(isInRoom ? 1 : 0);
                            SendResponse(Ws, gameResponse);
                            var room = new RoomStateChanged(Guid.NewGuid(),
                                DateTime.Now,
                                "RoomName",
                                "ZZZZZZZZ",
                                true,
                                new MatchmakingData(DateTime.Now, MatchmakingState.Unlocked, "QueueName", 1, 2, new Dictionary<string, string>(), null),
                                new List<UserInfo>
                                {
                                    new(UserId, 0, false, UserId.ToString()),
                                },
                                true,
                                false,
                                new Dictionary<string, string>());

                            WebSocketMockBackendSession.PlayerCurrentRoom = room.RoomId;
                            WebSocketMockBackendSession.Rooms[room.RoomId] = room;
                            SendResponse(Ws, room);
                            break;
                        }
                        case CreateRoom createRoom:
                        {
                            ThrowIfAlreadyInRoom(Ws, createRoom);
                            var (teamSize, teamCount) = GetQueueOrThrow(createRoom.QueueName, Ws, createRoom);

                            var room = new RoomStateChanged(Guid.NewGuid(),
                                DateTime.Now,
                                createRoom.RoomName,
                                "ZZZZZZZZ",
                                true,
                                new MatchmakingData(DateTime.Now, MatchmakingState.Unlocked, createRoom.QueueName, teamCount, teamSize, createRoom.CustomMatchmakingData, null),
                                new List<UserInfo>
                                {
                                    new(UserId, 0, false, UserId.ToString()),
                                },
                                createRoom.IsPrivate,
                                createRoom.IsEphemeral,
                                createRoom.CustomRoomData);

                            WebSocketMockBackendSession.PlayerCurrentRoom = room.RoomId;
                            WebSocketMockBackendSession.Rooms[room.RoomId] = room;

                            SendSuccessResponse(Ws, createRoom, room.RoomId);
                            UpdateTime(ref room);
                            SendResponse(Ws, room);

                            break;
                        }
                        case JoinWithRoomId joinWithRoomId:
                        {
                            ThrowIfAlreadyInRoom(Ws, joinWithRoomId);
                            var room = GetRoomOrThrow(joinWithRoomId.RoomId, Ws, joinWithRoomId);
                            ThrowIfTeamFull(joinWithRoomId.TeamIndex, room, Ws, joinWithRoomId);

                            WebSocketMockBackendSession.PlayerCurrentRoom = room.RoomId;
                            UpdateUsers(ref room, room.Users.Append(new UserInfo(UserId, joinWithRoomId.TeamIndex, false, UserId.ToString())));
                            SendSuccessResponse(Ws, joinWithRoomId, room.RoomId);
                            UpdateTime(ref room);
                            SendResponse(Ws, room);
                            break;
                        }
                        case JoinWithJoinCode joinWithJoinCode:
                        {
                            ThrowIfAlreadyInRoom(Ws, joinWithJoinCode);
                            var room = GetRoomOrThrow(joinWithJoinCode.JoinCode, Ws, joinWithJoinCode);
                            WebSocketMockBackendSession.PlayerCurrentRoom = room.RoomId;
                            UpdateUsers(ref room, room.Users.Append(new UserInfo(UserId, joinWithJoinCode.TeamIndex, false, UserId.ToString())));
                            SendSuccessResponse(Ws, joinWithJoinCode, room.RoomId);
                            UpdateTime(ref room);
                            SendResponse(Ws, room);
                            break;
                        }
                        case LeaveRoom leaveRoom:
                        {
                            ThrowIfNotInRoom(Ws, leaveRoom);
                            var room = GetRoomOrThrow(leaveRoom.RoomId, Ws, leaveRoom);

                            WebSocketMockBackendSession.PlayerCurrentRoom = null;
                            UpdateUsers(ref room, room.Users.Where(u => u.UserId != UserId));

                            SendSuccessResponse(Ws, leaveRoom);
                            UpdateTime(ref room);
                            SendResponse(Ws, new RoomWasLeft(room.RoomId, LeavingReason.UserLeft));
                            UpdateRoomOnList(Ws, room);
                            break;
                        }
                        case SetRoomParameters setRoomParameters:
                        {
                            ThrowIfNotInRoom(Ws, setRoomParameters);
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

                            SendSuccessResponse(Ws, setRoomParameters);
                            UpdateTime(ref room);
                            SendResponse(Ws, room);
                            UpdateRoomOnList(Ws, room);
                            break;
                        }
                        case ChangeTeam changeTeam:
                        {
                            ThrowIfNotInRoom(Ws, changeTeam);
                            var room = GetCurrentRoom();
                            ThrowIfTeamFull(changeTeam.TeamIndex, room, Ws, changeTeam);

                            UpdateUsers(ref room, room.Users.Select(u => u.UserId == UserId ? u with { TeamIndex = changeTeam.TeamIndex } : u));
                            SendSuccessResponse(Ws, changeTeam);
                            UpdateTime(ref room);
                            SendResponse(Ws, room);
                            break;
                        }
                        case SetReady setReady:
                        {
                            ThrowIfNotInRoom(Ws, setReady);
                            var room = GetCurrentRoom();
                            var user = room.Users.Single(u => u.UserId == UserId);
                            ThrowIfAlreadyReady(user, Ws, setReady);
                            UpdateUsers(ref room,
                                room.Users.Select(u => u.UserId == UserId ? u with
                                {
                                    IsReady = true
                                } : u));
                            SendSuccessResponse(Ws, setReady);
                            UpdateTime(ref room);
                            SendResponse(Ws, room);
                            break;
                        }
                        case SetUnready setUnready:
                        {
                            ThrowIfNotInRoom(Ws, setUnready);
                            var room = GetCurrentRoom();
                            var user = room.Users.Single(u => u.UserId == UserId);
                            ThrowIfAlreadyUnReady(user, Ws, setUnready);
                            UpdateUsers(ref room,
                                room.Users.Select(u => u.UserId == UserId ? u with
                                {
                                    IsReady = false
                                } : u));
                            SendSuccessResponse(Ws, setUnready);
                            UpdateTime(ref room);
                            SendResponse(Ws, room);
                            break;
                        }
                        case StartMatchmaking startMatchmaking:
                        {
                            ThrowIfAlreadyInMatchmaking(Ws, startMatchmaking);
                            cts = new CancellationTokenSource();
                            ThrowIfNotInRoom(Ws, startMatchmaking);
                            var room = GetCurrentRoom();
                            ThrowIfNoMatchmakingInRoom(Ws, room, startMatchmaking);
                            ThrowIfNotHost(Ws, room, startMatchmaking);
                            ThrowIfAllNotReady(Ws, room, startMatchmaking);
                            room = room with
                            {
                                MatchmakingData = room.MatchmakingData! with
                                {
                                    State = MatchmakingState.RequestingMatchmaking,
                                },
                            };
                            WebSocketMockBackendSession.Rooms[room.RoomId] = room;
                            SendSuccessResponse(Ws, startMatchmaking);
                            UpdateTime(ref room);
                            SendResponse(Ws, room);
                            try
                            {
                                SimulateMatchmaking(Ws, room, cts.Token);
                            }
                            catch (OperationCanceledException)
                            {
                                ElympicsLogger.Log($"[MOCK] Canceled matchmaking.");
                            }
                            break;
                        }
                        case CancelMatchmaking cancelMatchmaking:
                        {
                            ThrowIfNotInRoom(Ws, cancelMatchmaking);
                            var room = GetCurrentRoom();
                            ThrowIfNoMatchmakingStarted(Ws, cancelMatchmaking);
                            ThrowIfNoMatchmakingInRoom(Ws, room, cancelMatchmaking);
                            ThrowIfCannotCancel(Ws, room, cancelMatchmaking);
                            ThrowIfNotHost(Ws, room, cancelMatchmaking);
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
                            SendSuccessResponse(Ws, cancelMatchmaking);
                            UpdateTime(ref room);
                            SendResponse(Ws, room);
                            SimulateCancelling(Ws, room);
                            break;
                        }
                        case WatchRooms watchRooms:
                        {
                            ThrowIfMatchingWatchListState(Ws, watchRooms);
                            WebSocketMockBackendSession.TracksRoomList = true;
                            SendSuccessResponse(Ws, watchRooms);
                            SendResponse(Ws, new RoomListChanged(new List<ListedRoomChange>(WebSocketMockBackendSession.Rooms.Select(r => new ListedRoomChange(r.Key, CreatePublicRoomState(r.Value))))));
                            break;
                        }
                        case UnwatchRooms unwatchRooms:
                        {
                            ThrowIfMatchingWatchListState(Ws, unwatchRooms);
                            WebSocketMockBackendSession.TracksRoomList = false;
                            SendSuccessResponse(Ws, unwatchRooms);
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
            return Ws;
        }

        public static void SendPing()
        {
            SendResponse(WebSocket, new Ping());
        }

        public static void MakeAllPlayersReadyForRoom(Guid roomId)
        {
            var room = WebSocketMockBackendSession.Rooms[roomId];
            UpdateUsers(ref room,
                room.Users.Select(u => u.UserId != UserId ? u with
                {
                    IsReady = true
                } : u));
            UpdateTime(ref room);
            SendResponse(Ws, room);
        }

        public static void IsUserInRoom(bool inRoom)
        {
            isInRoom = inRoom;
            ElympicsLogger.Log($"[MOCK] User is assigned to room {inRoom}");
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
                SendResponse(Ws, room);
            UpdateRoomOnList(Ws, room);
        }

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
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
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

                await UniTask.Delay(TimeSpan.FromSeconds(5), DelayType.Realtime, PlayerLoopTiming.Update, ct);
                if (ct.IsCancellationRequested)
                    return;
                SetMatchmakingState(ref room, MatchmakingState.Matched);
                room = room with
                {
                    MatchmakingData = room.MatchmakingData! with
                    {
                        MatchData = GetDummyMatchData(room.Users.Select(x => x.UserId).ToList()),
                    },
                };
                UpdateTime(ref room);
                SendResponse(ws, room);

                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.Realtime, PlayerLoopTiming.Update, ct);
                if (ct.IsCancellationRequested)
                    return;
                SetMatchmakingState(ref room, MatchmakingState.Playing);
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
            SendFailResponse(ws, matchmakingOperation, ErrorBlame.UserError, ErrorKind.Unspecified, roomId, $"Cant's cancel matchmaking in states different than {MatchmakingState.Matchmaking} or {MatchmakingState.CancellingMatchmaking}.");
            throw new MessageHandledException();
        }
        private static void ThrowIfNoMatchmakingInRoom(IWebSocket ws, RoomStateChanged room, LobbyOperation matchmakingOperation, Guid? roomId = null)
        {
            if (room.MatchmakingData == null)
                SendFailResponse(ws, matchmakingOperation, ErrorBlame.UserError, ErrorKind.RoomWithoutMatchmaking, roomId, "Cant's start matchmaking in non-matchmaking room.");
            throw new MessageHandledException();
        }
        private static void ThrowIfNotHost(IWebSocket ws, RoomStateChanged room, LobbyOperation matchmakingOperation, Guid? roomId = null)
        {
            if (room.Users[0].UserId == UserId)
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
            ElympicsLogger.Log($"[MOCK] Sending response success on {lobbyOperation.GetType().Name}");
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
            var matchmakingData = new PublicMatchmakingData(roomState.MatchmakingData!.LastStateUpdate, roomState.MatchmakingData!.State, roomState.MatchmakingData.QueueName, roomState.MatchmakingData.TeamCount, roomState.MatchmakingData.TeamSize, roomState.MatchmakingData.CustomData);

            return new PublicRoomState(roomState.RoomId, roomState.LastUpdate, roomState.RoomName, roomState.HasPrivilegedHost, matchmakingData, roomState.Users, roomState.IsPrivate, roomState.CustomData);
        }
    }
}
