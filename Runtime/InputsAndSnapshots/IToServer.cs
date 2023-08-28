using MessagePack;

namespace Elympics
{
    [Union(0, typeof(ElympicsInput))]
    [Union(1, typeof(ElympicsInputList))]
    [Union(2, typeof(ElympicsRpcMessageList))]
    public interface IToServer
    { }
}
