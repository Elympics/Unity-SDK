namespace Elympics
{
    public class RoomNotConnectedException : ElympicsException
    {
        internal RoomNotConnectedException(string methodName)
            : base($"Method {methodName} cannot be used before the room receives its initial state.")
        { }
    }
}
