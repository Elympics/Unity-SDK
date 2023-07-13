namespace MatchTcpLibrary
{
    public interface IGameServerSerializer
    {
        byte[] Serialize(object obj);
        T Deserialize<T>(byte[] data);
    }
}
