using ProtoGameEngine;

namespace Proto.ProtoClient.Receivers
{
    internal interface IUnityGameEngineProtoReceiver
    {
        void Initialized();
        void OnInGameDataForPlayerOnReliableChannelGenerated(InGameDataForPlayerOnReliableChannelGeneratedMsg message);
        void OnInGameDataForPlayerOnUnreliableChannelGenerated(InGameDataForPlayerOnUnreliableChannelGeneratedMsg message);
        void OnInGameDataForSpectatorsOnReliableChannelGenerated(InGameDataForSpectatorsOnReliableChannelGeneratedMsg message);
        void OnInGameDataForSpectatorsOnUnreliableChannelGenerated(InGameDataForSpectatorsOnUnreliableChannelGeneratedMsg message);
        void OnGameEnded(NullableGameEndedMsg message);
    }
}
