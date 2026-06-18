namespace Elympics
{
    public class GameServerClosedException : ElympicsException
    {
        public GameServerClosedException() : base("Game server has closed")
        { }
    }
}
