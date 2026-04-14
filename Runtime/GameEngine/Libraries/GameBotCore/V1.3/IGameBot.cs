#nullable enable

namespace GameBotCore.V1._3
{
    public interface IGameBot : GameBotCore.V1._2.IGameBot
    {
        void Init3(BotConfiguration botConfiguration);
    }
}
