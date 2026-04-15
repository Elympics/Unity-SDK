using Google.Protobuf.WellKnownTypes;
using Proto.ProtoClient.NetworkClient;
using Proto.ProtoClient.Receivers;
using ProtoGameBot;
using ProtoLog;

namespace Proto.ProtoClient
{
	internal class UnityGameBotProtoClient : ProtoClient
	{
		private readonly IUnityGameBotProtoReceiver _receiver;
		private readonly ILogReceiver               _logReceiver;

		public UnityGameBotProtoClient(IProtoNetworkClient networkClient, IUnityGameBotProtoReceiver receiver, ILogReceiver logReceiver) : base(networkClient)
		{
			_receiver = receiver;
			_logReceiver = logReceiver;
		}

		protected override void OnMessage(Any message)
		{
			if (message.Is(InitializedMsg.Descriptor))
				_receiver.Initialized();
			if (message.Is(InGameDataForReliableChannelGeneratedMsg.Descriptor))
				_receiver.OnInGameDataForReliableChannelGenerated(message.Unpack<InGameDataForReliableChannelGeneratedMsg>());
			else if (message.Is(InGameDataForUnreliableChannelGeneratedMsg.Descriptor))
				_receiver.OnInGameDataForUnreliableChannelGenerated(message.Unpack<InGameDataForUnreliableChannelGeneratedMsg>());
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
		}
	}
}
