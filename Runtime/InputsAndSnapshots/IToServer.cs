using MessagePack;

namespace Elympics
{
    [Union(0, typeof(ElympicsInputList))]
    [Union(1, typeof(ElympicsRpcMessageList))]
    [Union(2, typeof(ElympicsReadyMessage))]
    public interface IToServer
    { }
}
