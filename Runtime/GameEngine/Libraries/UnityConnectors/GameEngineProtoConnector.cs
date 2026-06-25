using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using GameEngineCore;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Proto.ProtoClient;
using Proto.ProtoClient.NetworkClient;
using Proto.ProtoClient.Receivers;
using ProtoGameEngine;
using ProtoUnityGameEngine;

namespace UnityConnectors
{
    // ReSharper disable once UnusedType.Global
    public class GameEngineProtoConnector : IGameEngineProtoReceiver, IDisposable
    {
        private const int GameEnginePort = 50001;

        private readonly IGameEngine _gameEngine;
        private readonly ProtoConnector _protoConnector;

        private GameEngineProtoClient _client = null!;

        private readonly InitializedMsg _initializedMsg = new();


        public GameEngineProtoConnector(IGameEngine gameEngine)
        {
            _gameEngine = gameEngine;
            _protoConnector = new ProtoConnector(IPAddress.Loopback, GameEnginePort, CreateGameEngineProtoClient);
        }

        private ProtoClient CreateGameEngineProtoClient(TcpClient tcpClient)
        {
            Console.WriteLine($"{nameof(GameEngineProtoConnector)} creating proto client");
            var protoNetworkClient = new ProtoNetworkStreamClient(tcpClient.GetStream());
            _client = new GameEngineProtoClient(protoNetworkClient, this);
            _client.ReceivingEnded += Dispose;

            _gameEngine.Initialized += SendInitialized;
            _gameEngine.InGameDataForPlayerOnReliableChannelGenerated += OnInGameDataForPlayerOnReliableChannelGenerated;
            _gameEngine.InGameDataForPlayerOnUnreliableChannelGenerated += OnInGameDataForPlayerOnUnreliableChannelGenerated;
            _gameEngine.InGameDataForSpectatorsOnReliableChannelGenerated += OnInGameDataForSpectatorsOnReliableChannelGenerated;
            _gameEngine.InGameDataForSpectatorsOnUnreliableChannelGenerated += OnInGameDataForSpectatorsOnUnreliableChannelGenerated;
            _gameEngine.IntermediateScoreSubmitted += OnIntermediateScoreSubmitted;
            _gameEngine.SnapshotReplayInitialized += OnSnapshotReplayInitialized;
            _gameEngine.SnapshotDataForReplayGenerated += OnSnapshotDataFroReplayGenerated;
            _gameEngine.GameEnded += OnGameEnded;

            Console.WriteLine($"{nameof(GameEngineProtoConnector)} start receiving");
            _client.Receive();
            return _client;
        }

        private void OnIntermediateScoreSubmitted((Guid UserId, float Score, DateTimeOffset UtcTime, TimeSpan GameplayTime) arg) =>
            _client.Send(new SubmitScoreMsg
            {
                Score = arg.Score,
                UserId = arg.UserId.ToString(),
                Timestamp = arg.UtcTime.ToUnixTimeMilliseconds(),
                GameplayTime = (long)arg.GameplayTime.TotalMilliseconds,
            });


        private void OnSnapshotReplayInitialized(ArraySegment<byte> data)
        {
            _client.Send(new InitializeReplaySystemMsg { Data = ByteString.CopyFrom(data.Array, data.Offset, data.Count) });
        }

        private void OnSnapshotDataFroReplayGenerated(ArraySegment<byte> data)
        {
            _client.Send(new SnapshotDataForReplayGeneratedMsg { Data = ByteString.CopyFrom(data.Array, data.Offset, data.Count) });
        }

        public void Connect() => _protoConnector.Connect();

        private void OnInGameDataForPlayerOnReliableChannelGenerated(byte[] data, string userId) =>
            _client.Send(new InGameDataForPlayerOnReliableChannelGeneratedMsg { UserId = userId, Data = ByteString.CopyFrom(data) });

        private void OnInGameDataForPlayerOnUnreliableChannelGenerated(byte[] data, string userId) =>
            _client.Send(new InGameDataForPlayerOnUnreliableChannelGeneratedMsg { UserId = userId, Data = ByteString.CopyFrom(data) });

        private void OnInGameDataForSpectatorsOnReliableChannelGenerated(byte[] data) =>
            _client.Send(new InGameDataForSpectatorsOnReliableChannelGeneratedMsg { Data = ByteString.CopyFrom(data) });

        private void OnInGameDataForSpectatorsOnUnreliableChannelGenerated(byte[] data) =>
            _client.Send(new InGameDataForSpectatorsOnUnreliableChannelGeneratedMsg { Data = ByteString.CopyFrom(data) });

        private void OnGameEnded(ResultMatchUserDatas? result)
        {
            var msg = new NullableGameEndedMsg();
            if (result == null)
                msg.Null = NullValue.NullValue;
            else
                msg.Data = new UserDatas
                {
                    Data =
                    {
                        result.Select(x => new UserDatas.Types.UserData
                        {
                            UserId = x.UserId,
                            MatchmakerData = { x.MatchmakerData ?? Array.Empty<float>() },
                            GameEngineData = x.GameEngineData == null ? ByteString.Empty : ByteString.CopyFrom(x.GameEngineData),
                        }),
                    },
                };

            _client.Send(msg);
        }

        public void InGameDataFromPlayerReliable(InGameDataReliableReceivedMsg message)
        {
            _gameEngine.OnInGameDataFromPlayerReliableReceived(message.Data.ToByteArray(), message.UserId);
        }

        public void InGameDataFromPlayerUnreliable(InGameDataUnreliableReceivedMsg message)
        {
            _gameEngine.OnInGameDataFromPlayerUnreliableReceived(message.Data.ToByteArray(), message.UserId);
        }

        public void PlayerConnected(PlayerConnectedMsg message)
        {
            _gameEngine.OnPlayerConnected(message.UserId);
        }

        public void PlayerDisconnected(PlayerDisconnectedMsg message)
        {
            _gameEngine.OnPlayerDisconnected(message.UserId);
        }

        public void Tick(TickMsg request)
        {
            _protoConnector.UpdateLastUpdateFromGameServer();
            _gameEngine.Tick(request.Tick);
        }

        public void Init(InitialMatchDataMsg request)
        {
            Console.WriteLine("Initializing...");
            try
            {
                var initialMatchData = new InitialMatchData
                {
                    UserData = request.InitialMatchData.Select(x => new InitialMatchUserData
                    {
                        UserId = ParseWithFancyException(x.UserId, nameof(x.UserId)),
                        IsBot = x.IsBot,
                        BotDifficulty = x.BotDifficulty,
                        MatchmakerData = x.MatchmakerData.ToArray(),
                        GameEngineData = x.GameEngineData.ToByteArray(),
                        RoomId = OptionalFrom(x.HasRoomId, x.RoomId, nameof(x.RoomId)),
                        TeamIndex = x.TeamIndex,
                        Telegramid = x.TelegramId,
                        Address = x.Address,
                        Nickname = x.Nickname,
                        NicknameType = x.NicknameType,
                        CustomData = x.CustomData.ToDictionary(kv => kv.Key, kv => kv.Value),
                    })
                        .ToList(),
                    MatchId = OptionalFrom(request.HasMatchId, request.MatchId, nameof(request.MatchId)),
                    QueueName = request.QueueName,
                    RegionName = request.RegionName,
                    CustomRoomData = request.CustomRoomDatas.ToDictionary(
                        kv1 => ParseWithFancyException(kv1.Key, "CustomRoomDatas.RoomId"),
                        kv1 => (IReadOnlyDictionary<string, string>)kv1.Value.CustomRoomData_.ToDictionary(
                            kv2 => kv2.Key,
                            kv2 => kv2.Value)),
                    CustomMatchmakingData = request.CustomMatchmakingData.ToDictionary(kv => kv.Key, kv => kv.Value),
                    ExternalGameData = request.ExternalGameData.ToByteArray(),
                };

                _gameEngine.Initialize(initialMatchData);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception during initialization\n{e}");
                Thread.Sleep(1000);
                throw;
            }

            return;

            static Guid? OptionalFrom(bool has, string s, string name)
            {
                if (!has)
                    return null;

                return ParseWithFancyException(s, name);
            }

            static Guid ParseWithFancyException(string s, string name)
            {
                if (!Guid.TryParse(s, out var g))
                    throw new FormatException($"Invalid Guid format - {s} on field {name}");
                return g;
            }
        }

        private void SendInitialized()
        {
            _client.Send(_initializedMsg);
        }

        public void Dispose()
        {
            _protoConnector.Dispose();
            Environment.Exit(0);
        }
    }
}
