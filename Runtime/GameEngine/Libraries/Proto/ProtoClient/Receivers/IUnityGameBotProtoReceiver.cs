using ProtoGameBot;

namespace Proto.ProtoClient.Receivers
{
	internal interface IUnityGameBotProtoReceiver
	{
		void Initialized();
		void OnInGameDataForReliableChannelGenerated(InGameDataForReliableChannelGeneratedMsg message);
		void OnInGameDataForUnreliableChannelGenerated(InGameDataForUnreliableChannelGeneratedMsg message);
	}
}
