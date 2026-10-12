using Google.Protobuf.WellKnownTypes;
using Proto.ProtoClient.NetworkClient;
using Proto.ProtoClient.Receivers;
using ProtoGameEngine;
using ProtoLog;
using ProtoNtp;

namespace Proto.ProtoClient
{
    internal class UnityGameEngineProtoClient : ProtoClient
    {
        private readonly IUnityGameEngineProtoReceiver _receiver;
        private readonly ILogReceiver _logReceiver;
        private readonly IClientNtpReceiver _clientNtpReceiver;

        public UnityGameEngineProtoClient(IProtoNetworkClient networkClient, IUnityGameEngineProtoReceiver receiver, ILogReceiver logReceiver, IClientNtpReceiver clientNtpReceiver = null) : base(networkClient)
        {
            _receiver = receiver;
            _logReceiver = logReceiver;
            _clientNtpReceiver = clientNtpReceiver;
        }

        protected override void OnMessage(Any message)
        {
            if (message.Is(InitializedMsg.Descriptor))
                _receiver.Initialized();
            if (message.Is(InGameDataForPlayerOnReliableChannelGeneratedMsg.Descriptor))
                _receiver.OnInGameDataForPlayerOnReliableChannelGenerated(message.Unpack<InGameDataForPlayerOnReliableChannelGeneratedMsg>());
            else if (message.Is(InGameDataForPlayerOnUnreliableChannelGeneratedMsg.Descriptor))
                _receiver.OnInGameDataForPlayerOnUnreliableChannelGenerated(message.Unpack<InGameDataForPlayerOnUnreliableChannelGeneratedMsg>());
            if (message.Is(InGameDataForSpectatorsOnReliableChannelGeneratedMsg.Descriptor))
                _receiver.OnInGameDataForSpectatorsOnReliableChannelGenerated(message.Unpack<InGameDataForSpectatorsOnReliableChannelGeneratedMsg>());
            else if (message.Is(InGameDataForSpectatorsOnUnreliableChannelGeneratedMsg.Descriptor))
                _receiver.OnInGameDataForSpectatorsOnUnreliableChannelGenerated(message.Unpack<InGameDataForSpectatorsOnUnreliableChannelGeneratedMsg>());
            else if (message.Is(NullableGameEndedMsg.Descriptor))
                _receiver.OnGameEnded(message.Unpack<NullableGameEndedMsg>());
            else if (message.Is(LogVerboseMsg.Descriptor))
                _logReceiver.LogVerbose(message.Unpack<LogVerboseMsg>());
            else if (message.Is(LogDebugMsg.Descriptor))
                _logReceiver.LogDebug(message.Unpack<LogDebugMsg>());
            else if (message.Is(LogInfoMsg.Descriptor))
                _logReceiver.LogInfo(message.Unpack<LogInfoMsg>());
            else if (message.Is(LogWarningMsg.Descriptor))
                _logReceiver.LogWarning(message.Unpack<LogWarningMsg>());
            else if (message.Is(LogErrorMsg.Descriptor))
                _logReceiver.LogError(message.Unpack<LogErrorMsg>());
            else if (message.Is(LogFatalMsg.Descriptor))
                _logReceiver.LogFatal(message.Unpack<LogFatalMsg>());
            else if (message.Is(NtpMsg.Descriptor))
                _clientNtpReceiver?.OnNtp(message.Unpack<NtpMsg>());
        }
    }
}
