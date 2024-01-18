using Elympics.Rooms.Models;

namespace Elympics
{
    internal class RoomDataMemoryException : ElympicsException
    {
        public RoomDataMemoryException(string roomName, int dataSize) : base($"Data from room {roomName} is too large: {dataSize} bytes. Limit: {DictionaryExtensions.MaxDictMemorySize} bytes.")
        { }
    }
}
