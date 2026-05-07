using Google.Protobuf.WellKnownTypes;
using Proto.ProtoClient.NetworkClient;
using Proto.ProtoClient.Receivers;
using ProtoUnityGameBot;

namespace Proto.ProtoClient
{
    internal class GameBotProtoClient : ProtoClient
    {
        private readonly IGameBotProtoReceiver _receiver;

        public GameBotProtoClient(IProtoNetworkClient networkClient, IGameBotProtoReceiver receiver) : base(networkClient)
        {
            _receiver = receiver;
        }

        protected override void OnMessage(Any message)
        {
            if (message.Is(InGameDataUnreliableReceivedMsg.Descriptor))
                _receiver.InGameDataUnreliableReceived(message.Unpack<InGameDataUnreliableReceivedMsg>());
            else if (message.Is(InGameDataReliableReceivedMsg.Descriptor))
                _receiver.InGameDataReliableReceived(message.Unpack<InGameDataReliableReceivedMsg>());
            else if (message.Is(TickMsg.Descriptor))
                _receiver.Tick(message.Unpack<TickMsg>());
            else if (message.Is(BotConfiguration1Msg.Descriptor))
                _receiver.Init(message.Unpack<BotConfiguration1Msg>());
            else if (message.Is(BotConfiguration2Msg.Descriptor))
                _receiver.Init2(message.Unpack<BotConfiguration2Msg>());
        }
    }
}
