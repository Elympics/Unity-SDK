using MessagePack;

namespace Elympics
{
    [Union(0, typeof(ElympicsInputList))]
    [Union(1, typeof(ElympicsRpcMessageList))]
    public interface IToServer
    { }
}
