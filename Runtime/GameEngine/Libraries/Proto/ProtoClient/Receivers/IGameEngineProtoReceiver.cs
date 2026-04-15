using ProtoUnityGameEngine;

namespace Proto.ProtoClient.Receivers
{
	internal interface IGameEngineProtoReceiver
	{
		void InGameDataFromPlayerUnreliable(InGameDataUnreliableReceivedMsg message);
		void InGameDataFromPlayerReliable(InGameDataReliableReceivedMsg message);
		void Tick(TickMsg message);
		void PlayerConnected(PlayerConnectedMsg message);
		void PlayerDisconnected(PlayerDisconnectedMsg message);
		void Init(InitialMatchDataMsg message);
	}
}
