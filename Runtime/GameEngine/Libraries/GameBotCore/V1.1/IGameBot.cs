#nullable enable
using System;

namespace GameBotCore.V1._1
{
    public interface IGameBot
    {
        event Action<byte[]>? InGameDataForReliableChannelGenerated;

        event Action<byte[]>? InGameDataForUnreliableChannelGenerated;

        void OnInGameDataUnreliableReceived(byte[] data);

        void OnInGameDataReliableReceived(byte[] data);

        void Init(IGameBotLogger logger, BotConfiguration botConfiguration);

        void Tick(long tick);
    }
}
