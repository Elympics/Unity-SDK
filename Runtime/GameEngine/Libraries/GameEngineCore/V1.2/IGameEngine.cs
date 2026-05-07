#nullable enable
using System;

namespace GameEngineCore.V1._2
{
    public interface IGameEngine : _1.IGameEngine
    {
        event Action<byte[]>? InGameDataForSpectatorsOnReliableChannelGenerated;
        event Action<byte[]>? InGameDataForSpectatorsOnUnreliableChannelGenerated;
    }
}
