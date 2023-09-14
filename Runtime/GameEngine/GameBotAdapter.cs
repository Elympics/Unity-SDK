using System;
using GameBotCore.V1._1;
using MessagePack;
using BotConfiguration = GameBotCore.V1._3.BotConfiguration;
using IGameBot = GameBotCore.V1._3.IGameBot;

namespace Elympics
{
    public class GameBotAdapter : IGameBot
    {
        internal bool Initialized { get; private set; }
        internal ElympicsPlayer Player { get; private set; }

        public event Action<InitialMatchPlayerDataGuid> InitializedWithMatchPlayerData;

        public event Action<byte[]> InGameDataForReliableChannelGenerated;
        public event Action<byte[]> InGameDataForUnreliableChannelGenerated;

        public void OnInGameDataUnreliableReceived(byte[] data) =>
            ProcessReceivedInGameData(data);

        public void OnInGameDataReliableReceived(byte[] data) =>
            ProcessReceivedInGameData(data);

        private void ProcessReceivedInGameData(byte[] data)
        {
            var deserializedData = MessagePackSerializer.Deserialize<IFromServer>(data);
            if (deserializedData is ElympicsSnapshot snapshot)
                SnapshotReceived?.Invoke(snapshot);
            else if (deserializedData is ElympicsRpcMessageList rpcMessageList)
                RpcMessageListReceived?.Invoke(rpcMessageList);
        }

        public void Init(IGameBotLogger logger, GameBotCore.V1._1.BotConfiguration botConfiguration)
        { }

        public void Init2(GameBotCore.V1._2.BotConfiguration botConfiguration)
        { }

        public void Init3(BotConfiguration botConfiguration)
        {
            Player = ElympicsPlayer.FromIndex(botConfiguration.MatchPlayers.IndexOf(botConfiguration.UserId));
            InitializedWithMatchPlayerData?.Invoke(new InitialMatchPlayerDataGuid
            {
                Player = Player,
                IsBot = true,
                BotDifficulty = botConfiguration.Difficulty,
                MatchmakerData = botConfiguration.MatchmakerData,
                GameEngineData = botConfiguration.GameEngineData
            });
            Initialized = true;
        }

        public void Tick(long tick)
        {
        }

        internal event Action<ElympicsSnapshot> SnapshotReceived;
        internal event Action<ElympicsRpcMessageList> RpcMessageListReceived;
        internal void SendInput(ElympicsInput input) => SendDataToServer(input, false);
        internal void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList) => SendDataToServer(rpcMessageList, true);

        private void SendDataToServer(IToServer data, bool reliable)
        {
            var serializedData = MessagePackSerializer.Serialize(data);
            var sendData = reliable
                ? InGameDataForReliableChannelGenerated
                : InGameDataForUnreliableChannelGenerated;
            sendData?.Invoke(serializedData);
        }
    }
}
