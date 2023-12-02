using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GameEngineCore.V1._1;
using GameEngineCore.V1._3;
using MessagePack;
using IGameEngine = GameEngineCore.V1._4.IGameEngine;
using InitialMatchData = GameEngineCore.V1._4.InitialMatchData;

#pragma warning disable CS0618
#pragma warning disable CS0067

#nullable enable

namespace Elympics
{
    internal class GameEngineAdapter : IGameEngine
    {
        internal ElympicsPlayer[] Players { get; private set; } = Array.Empty<ElympicsPlayer>();

        public event Action<byte[], string>? InGameDataForPlayerOnReliableChannelGenerated;
        public event Action<byte[], string>? InGameDataForPlayerOnUnreliableChannelGenerated;
        public event Action<byte[]>? InGameDataForSpectatorsOnReliableChannelGenerated;
        public event Action<byte[]>? InGameDataForSpectatorsOnUnreliableChannelGenerated;
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
        private Dictionary<Guid, ElympicsPlayer> _userIdsToPlayers = null!;
        private Dictionary<ElympicsPlayer, Guid> _playersToUserIds = null!;

        private readonly int _playerInputBufferSize;

        internal readonly ConcurrentDictionary<ElympicsPlayer, ElympicsInput> LatestSimulatedTickInput = new();
        internal ConcurrentDictionary<ElympicsPlayer, ElympicsDataWithTickBuffer<ElympicsInput>> PlayerInputBuffers { get; } = new();


        internal GameEngineAdapter(ElympicsGameConfig elympicsGameConfig) =>
            _playerInputBufferSize = elympicsGameConfig.PredictionBufferSize;

        public void Init(IGameEngineLogger logger, GameEngineCore.V1._1.InitialMatchData initialMatchData) => throw new NotSupportedException();
        public void Init2(InitialMatchUserDatas initialMatchData) => throw new NotSupportedException();

        public void Initialize(InitialMatchData initialMatchData)
        {
            Players = Enumerable.Range(0, initialMatchData.UserData.Count).Select(ElympicsPlayer.FromIndex).ToArray();

            var userIds = initialMatchData.UserData.Select(userData => userData.UserId).ToList();

            _playersToUserIds = ElympicsPlayerAssociations.GetPlayersToUserIds(userIds);
            _userIdsToPlayers = ElympicsPlayerAssociations.GetUserIdsToPlayers(userIds);

            foreach (var userId in userIds)
                PlayerInputBuffers[_userIdsToPlayers[userId]] = new ElympicsDataWithTickBuffer<ElympicsInput>(_playerInputBufferSize);

            _initialMatchData = initialMatchData;
            ReceivedInitialMatchPlayerDatas?.Invoke((new InitialMatchPlayerDatasGuid(initialMatchData, _userIdsToPlayers), () => Initialized?.Invoke()));
        }

        public void OnInGameDataFromPlayerReliableReceived(byte[] data, string userId) =>
            ProcessReceivedInGameData(data, new Guid(userId));

        public void OnInGameDataFromPlayerUnreliableReceived(byte[] data, string userId) =>
            ProcessReceivedInGameData(data, new Guid(userId));

        private void ProcessReceivedInGameData(byte[] data, Guid userId)
        {
            var player = _userIdsToPlayers[userId];
            var deserializedData = MessagePackSerializer.Deserialize<IToServer>(data);

            if (deserializedData is ElympicsInput input)
                AddInputToBuffer(input, player);
            else if (deserializedData is ElympicsInputList inputList)
                foreach (var value in inputList.Values)
                    AddInputToBuffer(value, player);
            else if (deserializedData is ElympicsRpcMessageList rpcMessageList)
                RpcMessageListReceived?.Invoke(rpcMessageList);
        }

        private void AddInputToBuffer(ElympicsInput input, ElympicsPlayer player)
        {
            input.Player = player;
            if (!PlayerInputBuffers.TryGetValue(player, out var buffer))
            {
                ElympicsLogger.LogWarning($"Input buffer for {player} not found.");
                return;
            }

            _ = buffer.TryAddData(input);
        }

        internal void AddBotsOrClientsInServerInputToBuffer(ElympicsInput input, ElympicsPlayer player) => AddInputToBuffer(input, player);

        public void OnPlayerConnected(string userId) => PlayerConnected?.Invoke(_userIdsToPlayers[new Guid(userId)]);
        public void OnPlayerDisconnected(string userId) => PlayerDisconnected?.Invoke(_userIdsToPlayers[new Guid(userId)]);

        public void Tick(long tick)
        {
            /* Using Unity Update instead. */
        }

        internal void SetLatestSimulatedInputTick(ElympicsPlayer player, ElympicsInput elympicsInput)
        {
            LatestSimulatedTickInput[player] = elympicsInput;
        }

        internal void BroadcastDataToPlayers(IFromServer data, bool reliable)
        {
            var serializedData = MessagePackSerializer.Serialize(data);
            var sendData = reliable
                ? InGameDataForPlayerOnReliableChannelGenerated
                : InGameDataForPlayerOnUnreliableChannelGenerated;
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
            var sendData = reliable
                ? InGameDataForPlayerOnReliableChannelGenerated
                : InGameDataForPlayerOnUnreliableChannelGenerated;
            var userId = _playersToUserIds[player];
            var serializedData = MessagePackSerializer.Serialize(data);
            sendData?.Invoke(serializedData, userId.ToString());
        }

        internal void EndGame(ResultMatchPlayerDatas? result = null)
        {
            if (result == null)
            {
                GameEnded?.Invoke(null);
                return;
            }

            if (result.Count != Players.Length)
            {
                ElympicsLogger.LogError($"Invalid length of match result: expected {Players.Length}, "
                    + $"has {result.Count}.");
                GameEnded?.Invoke(null);
                return;
            }

            var matchResult = new ResultMatchUserDatas();
            for (var i = 0; i < result.Count; i++)
            {
                var userId = _playersToUserIds[Players[i]];
                matchResult.Add(new ResultMatchUserData
                {
                    UserId = userId.ToString(),
                    GameEngineData = result[i].GameEngineData,
                    MatchmakerData = result[i].MatchmakerData,
                });
            }

            GameEnded?.Invoke(matchResult);
        }

        public event Action? GameStarted;
        public event Action<List<GameEvent>>? GameEventsGathered;

        event Action<MatchResult> GameEngineCore.V1._1.IGameEngine.GameEnded
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }
    }
}
