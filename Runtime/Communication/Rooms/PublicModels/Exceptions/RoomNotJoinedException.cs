namespace Elympics
{
    public class RoomNotJoinedException : ElympicsException
    {
        internal RoomNotJoinedException(string methodName) : base($"Method {methodName} cannot be used on a room you are not a member of.")
        { }
    }
}
