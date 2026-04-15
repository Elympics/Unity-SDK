
using ProtoUnityGameBot;

namespace Proto.ProtoClient.Receivers
{
	internal interface IGameBotProtoReceiver
	{
		void InGameDataReliableReceived(InGameDataReliableReceivedMsg message);
		void InGameDataUnreliableReceived(InGameDataUnreliableReceivedMsg message);
		void Tick(TickMsg message);
		void Init(BotConfiguration1Msg message);
		void Init2(BotConfiguration2Msg message);
		void Init3(BotConfiguration3Msg message);
	}
}
