using Google.Protobuf.WellKnownTypes;
using Proto.ProtoClient.NetworkClient;
using Proto.ProtoClient.Receivers;
using ProtoNtp;
using ProtoUnityGameEngine;

namespace Proto.ProtoClient
{
    internal class GameEngineProtoClient : ProtoClient
    {
        private readonly IGameEngineProtoReceiver _receiver;
        private readonly IServerNtpReceiver _serverNtpReceiver;
        private readonly bool _allowReliable;
        private readonly bool _allowUnreliable;

        public GameEngineProtoClient(IProtoNetworkClient networkClient, IGameEngineProtoReceiver receiver, IServerNtpReceiver serverNtpReceiver = null, bool allowReliable = true, bool allowUnreliable = true) : base(networkClient)
        {
            _receiver = receiver;
            _serverNtpReceiver = serverNtpReceiver;
            _allowReliable = allowReliable;
            _allowUnreliable = allowUnreliable;
        }

        protected override void OnMessage(Any message)
        {
            if (_allowReliable)
            {
                if (message.Is(InGameDataReliableReceivedMsg.Descriptor))
                    _receiver.InGameDataFromPlayerReliable(message.Unpack<InGameDataReliableReceivedMsg>());
                else if (message.Is(TickMsg.Descriptor))
                    _receiver.Tick(message.Unpack<TickMsg>());
                else if (message.Is(PlayerConnectedMsg.Descriptor))
                    _receiver.PlayerConnected(message.Unpack<PlayerConnectedMsg>());
                else if (message.Is(PlayerDisconnectedMsg.Descriptor))
                    _receiver.PlayerDisconnected(message.Unpack<PlayerDisconnectedMsg>());
                else if (message.Is(InitialMatchDataMsg.Descriptor))
                    _receiver.Init(message.Unpack<InitialMatchDataMsg>());
            }

            if (_allowUnreliable)
            {
                if (message.Is(InGameDataUnreliableReceivedMsg.Descriptor))
                    _receiver.InGameDataFromPlayerUnreliable(message.Unpack<InGameDataUnreliableReceivedMsg>());
            }

            if (message.Is(NtpMsg.Descriptor))
                _serverNtpReceiver?.OnNtp(this, message.Unpack<NtpMsg>());
        }
    }
}
