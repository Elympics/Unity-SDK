using System;

namespace Elympics
{
    public class UnexpectedRoomResultException : ElympicsException
    {
        internal UnexpectedRoomResultException(Type expectedType, Type actualType)
            : base($"Received result of type {actualType.FullName} instead of {expectedType.FullName}.")
        { }
    }
}
