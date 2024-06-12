namespace Elympics
{
    public class RoomDisposedException : ElympicsException
    {
        internal RoomDisposedException(string methodName) : base($"Method {methodName} cannot be used on a disposed room.")
        { }
    }
}
