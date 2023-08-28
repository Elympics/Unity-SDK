using MessagePack;

namespace Elympics
{
    [Union(0, typeof(ElympicsSnapshot))]
    [Union(1, typeof(ElympicsRpcMessageList))]
    public interface IFromServer
    { }
}
