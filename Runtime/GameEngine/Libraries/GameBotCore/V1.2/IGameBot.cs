#nullable enable

namespace GameBotCore.V1._2
{
    public interface IGameBot : GameBotCore.V1._1.IGameBot
    {
        void Init2(BotConfiguration botConfiguration);
    }
}
