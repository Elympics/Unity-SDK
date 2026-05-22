using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Elympics.Core.Logger;
using GameEngineCore;
using MessagePack;
using UnityEngine.Assertions;

#pragma warning disable CS0618
#pragma warning disable CS0067

#nullable enable

namespace Elympics
{
    internal class GameEngineAdapter : IGameEngine
    {
        public event Action<byte[], string>? InGameDataForPlayerOnReliableChannelGenerated;
        public event Action<byte[], string>? InGameDataForPlayerOnUnreliableChannelGenerated;
        public event Action<byte[]>? InGameDataForSpectatorsOnReliableChannelGenerated;
        public event Action<byte[]>? InGameDataForSpectatorsOnUnreliableChannelGenerated;
        public event Action<ArraySegment<byte>>? SnapshotDataForReplayGenerated;
        public event Action<ArraySegment<byte>>? SnapshotReplayInitialized;
        public event Action<ResultMatchUserDatas?>? GameEnded;
        public event Action<ElympicsPlayer>? PlayerConnected;
        public event Action<ElympicsPlayer>? PlayerDisconnected;
        public event Action? Initialized;

        /// <remarks>
        /// Invokes <see cref="Initialized"/> every time action passed in OnInitialized is called, but only the first call is meaningful.
        /// There should be only one subscriber calling OnInitialized exactly once after it initializes.
        /// </remarks>
        internal event Action<(InitialMatchPlayerDatasGuid Data, Action OnInitialized)>? ReceivedInitialMatchPlayerDatas;

        public event Action<ElympicsRpcMessageList>? RpcMessageListReceived;

        private InitialMatchData _initialMatchData = null!;
        private int UserCount => _initialMatchData.UserData.Count;
        private Dictionary<Guid, ElympicsPlayer> _userIdsToPlayers = null!;

        private readonly int _playerInputBufferSize;

        internal readonly ConcurrentDictionary<ElympicsPlayer, ElympicsInput> LatestSimulatedTickInput = new();
        internal ConcurrentDictionary<ElympicsPlayer, ElympicsDataWithTickBuffer<ElympicsInput>> PlayerInputBuffers { get; } = new();

        internal GameEngineAdapter(ElympicsGameConfig elympicsGameConfig) =>
            _playerInputBufferSize = elympicsGameConfig.PredictionBufferSize;

        public void Initialize(InitialMatchData initialMatchData) => Initialize(initialMatchData, false);

        public void Initialize(InitialMatchData initialMatchData, bool isReplay)
        {
            _initialMatchData = initialMatchData;

            var userIds = initialMatchData.UserData.Select(userData => userData.UserId).ToList();
            _userIdsToPlayers = ElympicsPlayerAssociations.GetUserIdsToPlayers(userIds);

            foreach (var userId in userIds)
                PlayerInputBuffers[_userIdsToPlayers[userId]] = new ElympicsDataWithTickBuffer<ElympicsInput>(_playerInputBufferSize);

            var world = Replication.ElympicsWorld.Current;
            Assert.IsNotNull(world);
            if (world != null)
                for (var i = 0; i < UserCount; i++)
                    world.RegisterPlayer(i);

            ReceivedInitialMatchPlayerDatas?.Invoke((new InitialMatchPlayerDatasGuid(initialMatchData, _userIdsToPlayers, isReplay), () => Initialized?.Invoke()));
        }

        public void OnInGameDataFromPlayerReliableReceived(byte[] data, string userId) =>
            ProcessReceivedInGameData(data, new Guid(userId));

        public void OnInGameDataFromPlayerUnreliableReceived(byte[] data, string userId) =>
            ProcessReceivedInGameData(data, new Guid(userId));

        private void ProcessReceivedInGameData(byte[] data, Guid userId)
        {
            var player = _userIdsToPlayers[userId];
            var deserializedData = MessagePackSerializer.Deserialize<IToServer>(data);

            if (deserializedData is ElympicsInputList inputList)
                ProcessReceivedInputList(inputList, player);
            else if (deserializedData is ElympicsRpcMessageList rpcMessageList)
            {
                for (var i = rpcMessageList.Count - 1; i >= 0; i--)
                {
                    var sentTick = rpcMessageList[i].SentOnTick;
                    var sender = rpcMessageList[i].Sender;
                    if ((int)player != sender)
                    {
                        rpcMessageList.RemoveAt(i);
                        ElympicsLogger.LogWarning($"[RPC] RPC from Tick {sentTick} Sender {sender} userId: {_initialMatchData.UserData[sender].UserId}"
                                                  + $" is not the same as socket owner {player} userId: {userId}. RPC will be not invoked.");
                    }
                }

                if (rpcMessageList.Count > 0)
                    RpcMessageListReceived?.Invoke(rpcMessageList);
            }
        }

        private void ProcessReceivedInputList(ElympicsInputList inputList, ElympicsPlayer player)
        {
            var playerIndex = (int)player;
            // Enqueue update for thread-safe drain at tick start
            var world = Replication.ElympicsWorld.Current;
            world?.PlayerUpdateQueue.Enqueue(playerIndex, inputList.LastReceivedSnapshot);

            foreach (var value in inputList.Values)
                AddInputToBuffer(value, player, value.Tick == inputList.Values[^1].Tick);
        }

        private void AddInputToBuffer(ElympicsInput input, ElympicsPlayer player, bool latestInput)
        {
            input.Player = player;
            if (!PlayerInputBuffers.TryGetValue(player, out var buffer))
            {
                ElympicsLogger.LogWarning($"Input buffer for {player} not found.");
                return;
            }

            var added = buffer.TryAddData(input);

            if (!added && latestInput)
                ElympicsLogger.LogWarning($"Input for Tick {input.Tick} from player {player} was not added to input buffer because it was not in range [{buffer.MinTick}, {buffer.MaxTick}].");
        }

        internal void AddBotsOrClientsInServerInputToBuffer(ElympicsInput input) => AddInputToBuffer(input, input.Player, true);

        public void OnPlayerConnected(string userId)
        {
            var player = _userIdsToPlayers[new Guid(userId)];
            var world = Replication.ElympicsWorld.Current;
            world?.ActivatePlayer((int)player);
            PlayerConnected?.Invoke(player);
        }

        public void OnPlayerDisconnected(string userId)
        {
            var player = _userIdsToPlayers[new Guid(userId)];
            PlayerDisconnected?.Invoke(player);
            var world = Replication.ElympicsWorld.Current;
            world?.DeactivatePlayer((int)player);
        }

        public void Tick(long tick)
        {
            /* Using Unity Update instead. */
        }

        public event Action<(Guid UserId, float Score, DateTimeOffset UtcTime, TimeSpan GameplayTime)>? IntermediateScoreSubmitted;

        internal void SetLatestSimulatedInputTick(ElympicsPlayer player, ElympicsInput elympicsInput)
        {
            LatestSimulatedTickInput[player] = elympicsInput;
        }

        #region Replays

        internal void SaveSnapshotForReplay(ArraySegment<byte> data) => SnapshotDataForReplayGenerated?.Invoke(data);

        internal void SaveReplayInitData(ArraySegment<byte> initData) => SnapshotReplayInitialized?.Invoke(initData);

        #endregion

        internal void BroadcastDataToPlayers(IFromServer data, bool reliable)
        {
            var serializedData = MessagePackSerializer.Serialize(data);
            var sendData = reliable ? InGameDataForPlayerOnReliableChannelGenerated : InGameDataForPlayerOnUnreliableChannelGenerated;
            foreach (var userData in _initialMatchData.UserData)
                sendData?.Invoke(serializedData, userData.UserId.ToString());
        }

        internal void SendSnapshotsToPlayers(Dictionary<ElympicsPlayer, ElympicsSnapshot> snapshotPerPlayer)
        {
            foreach (var (player, snapshot) in snapshotPerPlayer)
                SendDataToPlayer(snapshot, player, false);
        }

        private void SendDataToPlayer(IFromServer data, ElympicsPlayer player, bool reliable)
        {
            var sendData = reliable ? InGameDataForPlayerOnReliableChannelGenerated : InGameDataForPlayerOnUnreliableChannelGenerated;
            var userId = _initialMatchData.UserData[(int)player].UserId;
            var serializedData = MessagePackSerializer.Serialize(data);
            sendData?.Invoke(serializedData, userId.ToString());
        }

        internal void SubmitIntermediateScore(float score, ElympicsPlayer player, DateTime utcTime, TimeSpan gameplayTime)
        {
            var userId = _initialMatchData.UserData[(int)player].UserId;
            IntermediateScoreSubmitted?.Invoke((userId, score, utcTime, gameplayTime));
        }

        internal void EndGame(ResultMatchPlayerDatas? result = null)
        {
            if (result == null)
            {
                GameEnded?.Invoke(null);
                return;
            }

            if (result.Count != UserCount)
            {
                ElympicsLogger.LogError($"Invalid length of match result: expected {UserCount}, " + $"has {result.Count}.");
                GameEnded?.Invoke(null);
                return;
            }

            var matchResult = new ResultMatchUserDatas();
            for (var i = 0; i < result.Count; i++)
            {
                var userId = _initialMatchData.UserData[i].UserId;
                matchResult.Add(new ResultMatchUserData
                {
                    UserId = userId.ToString(),
                    GameEngineData = result[i].GameEngineData,
                    MatchmakerData = result[i].MatchmakerData,
                });
            }

            GameEnded?.Invoke(matchResult);
        }
    }
}
