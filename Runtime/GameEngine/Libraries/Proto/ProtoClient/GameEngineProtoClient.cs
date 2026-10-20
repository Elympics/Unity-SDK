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

        /// <summary>The player who joined over this client and hasn't left yet, or null.</summary>
        public string PlayerUserId
        {
            get => _playerUserId;
            private set => _playerUserId = value;
        }

        // Written on the receive thread, read from other threads.
        private volatile string _playerUserId;

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
                {
                    var playerConnected = message.Unpack<PlayerConnectedMsg>();
                    PlayerUserId = playerConnected.UserId;
                    _receiver.PlayerConnected(playerConnected);
                }
                else if (message.Is(PlayerDisconnectedMsg.Descriptor))
                {
                    PlayerUserId = null;
                    _receiver.PlayerDisconnected(message.Unpack<PlayerDisconnectedMsg>());
                }
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
