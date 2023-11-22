using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GameEngineCore.V1._1;
using GameEngineCore.V1._3;
using MessagePack;
using IGameEngine = GameEngineCore.V1._3.IGameEngine;

#pragma warning disable CS0618
#pragma warning disable CS0067

namespace Elympics
{
    internal class GameEngineAdapter : IGameEngine
    {
        internal ElympicsPlayer[] Players { get; private set; } = Array.Empty<ElympicsPlayer>();

        public event Action<byte[], string> InGameDataForPlayerOnReliableChannelGenerated;
        public event Action<byte[], string> InGameDataForPlayerOnUnreliableChannelGenerated;
        public event Action<byte[]> InGameDataForSpectatorsOnReliableChannelGenerated;
        public event Action<byte[]> InGameDataForSpectatorsOnUnreliableChannelGenerated;
        public event Action<ResultMatchUserDatas> GameEnded;
        public event Action<ElympicsPlayer> PlayerConnected;
        public event Action<ElympicsPlayer> PlayerDisconnected;

        /// <remarks>
        /// Invokes <see cref="Initialized"/> every time action passed in OnInitialized is called, but only the first call is meaningful.
        /// There should be only one subscriber calling OnInitialized exactly once after it initializes.
        /// </remarks>
        internal event Action<(InitialMatchPlayerDatasGuid Data, Action OnInitialized)> ReceivedInitialMatchPlayerDatas;
        internal event Action Initialized;

        public event Action<ElympicsRpcMessageList> RpcMessageListReceived;

        private InitialMatchUserDatas _initialMatchUserDatas;
        private Dictionary<string, ElympicsPlayer> _userIdsToPlayers;
        private Dictionary<ElympicsPlayer, string> _playersToUserIds;

        private readonly int _playerInputBufferSize;

        internal readonly ConcurrentDictionary<ElympicsPlayer, ElympicsInput> LatestSimulatedTickInput = new();
        internal ConcurrentDictionary<ElympicsPlayer, ElympicsDataWithTickBuffer<ElympicsInput>> PlayerInputBuffers { get; } = new();


        internal GameEngineAdapter(ElympicsGameConfig elympicsGameConfig) =>
            _playerInputBufferSize = elympicsGameConfig.PredictionBufferSize;

        public void Init(IGameEngineLogger logger, InitialMatchData initialMatchData)
        { }

        public void Init2(InitialMatchUserDatas initialMatchUserDatas)
        {
            Players = Enumerable.Range(0, initialMatchUserDatas.Count).Select(ElympicsPlayer.FromIndex).ToArray();

            var userIds = initialMatchUserDatas.Select(userData => userData.UserId).ToList();

            _playersToUserIds = ElympicsPlayerAssociations.GetPlayersToUserIds(userIds);
            _userIdsToPlayers = ElympicsPlayerAssociations.GetUserIdsToPlayers(userIds);

            foreach (var userId in userIds)
                PlayerInputBuffers[_userIdsToPlayers[userId]] = new ElympicsDataWithTickBuffer<ElympicsInput>(_playerInputBufferSize);

            _initialMatchUserDatas = initialMatchUserDatas;
            ReceivedInitialMatchPlayerDatas?.Invoke((new InitialMatchPlayerDatasGuid(initialMatchUserDatas.Select(x => new InitialMatchPlayerDataGuid
            {
                Player = _userIdsToPlayers[x.UserId],
                UserId = new Guid(x.UserId),
                IsBot = x.IsBot,
                BotDifficulty = x.BotDifficulty,
                GameEngineData = x.GameEngineData,
                MatchmakerData = x.MatchmakerData,
            }).ToList()), () => Initialized?.Invoke()));
        }

        public void OnInGameDataFromPlayerReliableReceived(byte[] data, string userId) =>
            ProcessReceivedInGameData(data, userId);

        public void OnInGameDataFromPlayerUnreliableReceived(byte[] data, string userId) =>
            ProcessReceivedInGameData(data, userId);

        private void ProcessReceivedInGameData(byte[] data, string userId)
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

        public void OnPlayerConnected(string userId) => PlayerConnected?.Invoke(_userIdsToPlayers[userId]);
        public void OnPlayerDisconnected(string userId) => PlayerDisconnected?.Invoke(_userIdsToPlayers[userId]);

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
            foreach (var userData in _initialMatchUserDatas)
                sendData?.Invoke(serializedData, userData.UserId);
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
            sendData?.Invoke(serializedData, userId);
        }

        internal void EndGame(ResultMatchPlayerDatas result = null)
        {
            if (result == null)
            {
                GameEnded?.Invoke(null);
                return;
            }

            if (result.Count != _initialMatchUserDatas.Count)
            {
                ElympicsLogger.LogError($"Invalid length of match result: expected {_initialMatchUserDatas.Count}, "
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
                    UserId = userId,
                    GameEngineData = result[i].GameEngineData,
                    MatchmakerData = result[i].MatchmakerData,
                });
            }

            GameEnded?.Invoke(matchResult);
        }

        public event Action GameStarted;
        public event Action<List<GameEvent>> GameEventsGathered;

        event Action<MatchResult> GameEngineCore.V1._1.IGameEngine.GameEnded
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }
    }
}
